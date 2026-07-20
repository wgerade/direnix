using Direnix.Core.Collection;
using Direnix.Core.Findings;

namespace Direnix.Core.Rules;

/// <summary>
/// Utilitarios compartilhados pelas regras. Titulo/categoria/acao vem do
/// <see cref="RuleCatalog"/>; as regras fornecem severidade, objeto e evidencia.
/// </summary>
internal static class RuleHelpers
{
    public static Finding Build(
        RuleContext ctx,
        string ruleId,
        Severity severity,
        string objectKey,
        string objectDisplay,
        int businessMultipliers,
        IReadOnlyDictionary<string, string> evidence)
    {
        var definition = RuleCatalog.Get(ruleId);
        var score = RiskScoring.Compute(severity, businessMultipliers);
        return new Finding(
            Finding.BuildStableKey(ruleId, objectKey),
            ruleId,
            definition.Title,
            definition.Category,
            severity,
            definition.Action,
            FindingStatus.New,
            score,
            ctx.DomainName,
            objectKey,
            objectDisplay,
            evidence);
    }

    public static Finding Build(
        RuleContext ctx,
        string ruleId,
        Severity severity,
        CollectedObject obj,
        int businessMultipliers,
        IReadOnlyDictionary<string, string> evidence)
    {
        // Enriquecimento padrao para o administrador identificar o objeto: nome de
        // exibicao amigavel e o SID (quando houver). Nao sobrescreve o que a regra ja definiu.
        var enriched = new Dictionary<string, string>(evidence, StringComparer.OrdinalIgnoreCase);
        var displayName = obj.Value("displayName");
        if (!string.IsNullOrWhiteSpace(displayName) && !enriched.ContainsKey("displayName"))
        {
            enriched["displayName"] = displayName!;
        }
        if (!string.IsNullOrWhiteSpace(obj.ObjectSid) && !enriched.ContainsKey("objectSid"))
        {
            enriched["objectSid"] = obj.ObjectSid!;
        }

        return Build(ctx, ruleId, severity, obj.ObjectKey, obj.DisplayName, businessMultipliers, enriched);
    }
}

/// <summary>ADCLN-USER-STALE-001 - usuario habilitado sem atividade recente.</summary>
public sealed class StaleUserRule : IAdHygieneRule
{
    public string RuleId => "ADCLN-USER-STALE-001";

    public IEnumerable<Finding> Evaluate(RuleContext ctx)
    {
        foreach (var user in ctx.OfType(AdObjectType.User))
        {
            if (AdAttributes.IsEnabled(user.Value("userAccountControl")) != true ||
                RuleContext.IsWellKnownBuiltIn(user))
            {
                continue;
            }

            var lastLogon = AdAttributes.ParseFileTime(user.Value("lastLogonTimestamp"));
            var reference = lastLogon ?? AdAttributes.ParseGeneralizedTime(user.Value("whenCreated"));
            var age = AdAttributes.AgeInDays(reference, ctx.AsOf);
            if (age is null || age < ctx.Thresholds.StaleUserDays)
            {
                continue;
            }

            var privileged = ctx.IsPrivileged(user);
            yield return RuleHelpers.Build(
                ctx,
                RuleId,
                privileged ? Severity.High : Severity.Medium,
                user,
                privileged ? ctx.Criticality.Tier0Multiplier : 0,
                new Dictionary<string, string>
                {
                    ["ageDays"] = age.Value.ToString(),
                    ["lastLogon"] = lastLogon?.ToString("u") ?? "nunca",
                    ["thresholdDays"] = ctx.Thresholds.StaleUserDays.ToString(),
                    ["privileged"] = privileged ? "true" : "false",
                    ["distinguishedName"] = user.DistinguishedName
                });
        }
    }
}

/// <summary>ADCLN-COMP-STALE-003 - computador habilitado sem atividade/senha recente.</summary>
public sealed class StaleComputerRule : IAdHygieneRule
{
    public string RuleId => "ADCLN-COMP-STALE-003";

    public IEnumerable<Finding> Evaluate(RuleContext ctx)
    {
        foreach (var computer in ctx.OfType(AdObjectType.Computer))
        {
            if (AdAttributes.IsEnabled(computer.Value("userAccountControl")) != true ||
                ctx.DomainControllerDns.Contains(computer.DistinguishedName) ||
                RuleContext.IsWellKnownBuiltIn(computer))
            {
                continue;
            }

            var lastLogon = AdAttributes.ParseFileTime(computer.Value("lastLogonTimestamp"));
            var pwdLastSet = AdAttributes.ParseFileTime(computer.Value("pwdLastSet"));
            var mostRecent = new[] { lastLogon, pwdLastSet }
                .Where(value => value is not null)
                .Select(value => value!.Value)
                .DefaultIfEmpty()
                .Max();
            var reference = mostRecent == default
                ? AdAttributes.ParseGeneralizedTime(computer.Value("whenCreated"))
                : mostRecent;

            var age = AdAttributes.AgeInDays(reference, ctx.AsOf);
            if (age is null || age < ctx.Thresholds.StaleComputerDays)
            {
                continue;
            }

            yield return RuleHelpers.Build(
                ctx,
                RuleId,
                Severity.Medium,
                computer,
                0,
                new Dictionary<string, string>
                {
                    ["ageDays"] = age.Value.ToString(),
                    ["lastLogon"] = lastLogon?.ToString("u") ?? "nunca",
                    ["pwdLastSet"] = pwdLastSet?.ToString("u") ?? "desconhecido",
                    ["operatingSystem"] = computer.Value("operatingSystem") ?? "desconhecido",
                    ["thresholdDays"] = ctx.Thresholds.StaleComputerDays.ToString(),
                    ["distinguishedName"] = computer.DistinguishedName
                });
        }
    }
}

/// <summary>ADCLN-USER-DISABLED-RETENTION-002 - usuario desabilitado retido alem da politica.</summary>
public sealed class DisabledUserRetentionRule : IAdHygieneRule
{
    public string RuleId => "ADCLN-USER-DISABLED-RETENTION-002";

    public IEnumerable<Finding> Evaluate(RuleContext ctx)
    {
        foreach (var user in ctx.OfType(AdObjectType.User))
        {
            if (AdAttributes.IsEnabled(user.Value("userAccountControl")) != false ||
                RuleContext.IsWellKnownBuiltIn(user))
            {
                continue;
            }

            var changed = AdAttributes.ParseGeneralizedTime(user.Value("whenChanged"));
            var age = AdAttributes.AgeInDays(changed, ctx.AsOf);
            if (age is null || age < ctx.Thresholds.DisabledObjectRetentionDays)
            {
                continue;
            }

            yield return RuleHelpers.Build(
                ctx,
                RuleId,
                Severity.Low,
                user,
                0,
                new Dictionary<string, string>
                {
                    ["ageDays"] = age.Value.ToString(),
                    ["whenChanged"] = changed?.ToString("u") ?? "desconhecido",
                    ["thresholdDays"] = ctx.Thresholds.DisabledObjectRetentionDays.ToString(),
                    ["distinguishedName"] = user.DistinguishedName
                });
        }
    }
}

/// <summary>ADAUTH-MAQ-009 - ms-DS-MachineAccountQuota acima do valor aprovado.</summary>
public sealed class MachineAccountQuotaRule : IAdHygieneRule
{
    public string RuleId => "ADAUTH-MAQ-009";

    public IEnumerable<Finding> Evaluate(RuleContext ctx)
    {
        var domain = ctx.DomainRoot;
        if (domain is null)
        {
            yield break;
        }

        var measured = AdAttributes.ParseInt(domain.Value("ms-DS-MachineAccountQuota"));
        if (measured is null || measured <= ctx.Thresholds.MachineAccountQuotaExpected)
        {
            yield break;
        }

        yield return RuleHelpers.Build(
            ctx,
            RuleId,
            Severity.High,
            domain.ObjectKey,
            ctx.DomainName,
            0,
            new Dictionary<string, string>
            {
                ["measuredValue"] = measured.Value.ToString(),
                ["expectedValue"] = ctx.Thresholds.MachineAccountQuotaExpected.ToString(),
                ["domain"] = ctx.DomainName
            });
    }
}

/// <summary>ADAUTH-KRB-PREAUTH-006 - Kerberos pre-auth desabilitado (AS-REP roasting).</summary>
public sealed class KerberosPreauthRule : IAdHygieneRule
{
    public string RuleId => "ADAUTH-KRB-PREAUTH-006";

    public IEnumerable<Finding> Evaluate(RuleContext ctx)
    {
        foreach (var user in ctx.OfType(AdObjectType.User))
        {
            if (!AdAttributes.HasFlag(user.Value("userAccountControl"), UserAccountControlFlags.DontRequirePreauth))
            {
                continue;
            }

            var privileged = ctx.IsPrivileged(user);
            yield return RuleHelpers.Build(
                ctx,
                RuleId,
                privileged ? Severity.Critical : Severity.High,
                user,
                privileged ? ctx.Criticality.Tier0Multiplier : 0,
                new Dictionary<string, string>
                {
                    ["privileged"] = privileged ? "true" : "false",
                    ["distinguishedName"] = user.DistinguishedName
                });
        }
    }
}

/// <summary>ADSVC-UNCONSTR-004 - unconstrained delegation habilitado (exceto DCs).</summary>
public sealed class UnconstrainedDelegationRule : IAdHygieneRule
{
    public string RuleId => "ADSVC-UNCONSTR-004";

    public IEnumerable<Finding> Evaluate(RuleContext ctx)
    {
        foreach (var obj in ctx.Objects)
        {
            if (obj.ObjectType is not (AdObjectType.User or AdObjectType.Computer))
            {
                continue;
            }

            if (!AdAttributes.HasFlag(obj.Value("userAccountControl"), UserAccountControlFlags.TrustedForDelegation) ||
                ctx.DomainControllerDns.Contains(obj.DistinguishedName))
            {
                continue;
            }

            yield return RuleHelpers.Build(
                ctx,
                RuleId,
                Severity.Critical,
                obj,
                0,
                new Dictionary<string, string>
                {
                    ["objectType"] = obj.ObjectType.ToString(),
                    ["distinguishedName"] = obj.DistinguishedName
                });
        }
    }
}

/// <summary>ADPRV-T0-GROUPS-001 - membros diretos em grupos Tier 0 / privilegiados.</summary>
public sealed class PrivilegedGroupExposureRule : IAdHygieneRule
{
    public string RuleId => "ADPRV-T0-GROUPS-001";

    public IEnumerable<Finding> Evaluate(RuleContext ctx)
    {
        foreach (var (memberDn, groups) in ctx.PrivilegedMembership)
        {
            var member = ctx.DnIndex.GetValueOrDefault(memberDn);

            // O Administrator interno (-500) e membro de Domain Admins por design
            // e nao pode ser removido; reporta-lo so gera ruido. Demais contas
            // privilegiadas (RID >= 1000) seguem reportadas.
            if (member is not null && RuleContext.IsWellKnownBuiltIn(member))
            {
                continue;
            }

            var display = member?.DisplayName ?? memberDn;
            var objectKey = member?.ObjectKey ?? $"dn:{memberDn}";

            yield return RuleHelpers.Build(
                ctx,
                RuleId,
                Severity.High,
                objectKey,
                display,
                ctx.Criticality.Tier0Multiplier,
                new Dictionary<string, string>
                {
                    ["privilegedGroups"] = string.Join(", ", groups),
                    ["objectType"] = member?.ObjectType.ToString() ?? "Unknown",
                    ["distinguishedName"] = memberDn,
                    ["sam"] = member?.SamAccountName ?? display,
                    ["displayName"] = member?.Value("displayName") ?? display,
                    ["objectSid"] = member?.ObjectSid ?? "desconhecido"
                });
        }
    }
}

/// <summary>ADGOV-RECYCLEBIN-001 - Lixeira do AD desabilitada ou com retencao curta.</summary>
public sealed class RecycleBinRule : IAdHygieneRule
{
    public string RuleId => "ADGOV-RECYCLEBIN-001";

    public IEnumerable<Finding> Evaluate(RuleContext ctx)
    {
        var domain = ctx.DomainRoot;
        if (domain is null)
        {
            yield break;
        }

        // Atributos sinteticos anexados pelo coletor a partir do Configuration NC.
        var enabledRaw = domain.Value("recycleBinEnabled");
        if (enabledRaw is null)
        {
            // Coletor nao conseguiu ler a config (permissao/erro). Nao inventa achado.
            yield break;
        }

        var enabled = string.Equals(enabledRaw, "true", StringComparison.OrdinalIgnoreCase);
        var retention = AdAttributes.ParseInt(domain.Value("msDS-DeletedObjectLifetime"))
            ?? AdAttributes.ParseInt(domain.Value("tombstoneLifetime"));
        var threshold = ctx.Thresholds.RecycleBinMinRetentionDays;

        if (!enabled)
        {
            yield return RuleHelpers.Build(
                ctx,
                RuleId,
                Severity.High,
                domain.ObjectKey,
                ctx.DomainName,
                0,
                new Dictionary<string, string>
                {
                    ["enabled"] = "false",
                    ["retentionDays"] = retention?.ToString() ?? "desconhecido",
                    ["thresholdDays"] = threshold.ToString(),
                    ["domain"] = ctx.DomainName
                });
            yield break;
        }

        if (retention is not null && retention < threshold)
        {
            yield return RuleHelpers.Build(
                ctx,
                RuleId,
                Severity.Medium,
                domain.ObjectKey,
                ctx.DomainName,
                0,
                new Dictionary<string, string>
                {
                    ["enabled"] = "true",
                    ["retentionDays"] = retention.Value.ToString(),
                    ["thresholdDays"] = threshold.ToString(),
                    ["domain"] = ctx.DomainName
                });
        }
    }
}

/// <summary>ADPRV-KRBTGT-AGE-010 - krbtgt sem rotacao documentada.</summary>
public sealed class KrbtgtAgeRule : IAdHygieneRule
{
    public string RuleId => "ADPRV-KRBTGT-AGE-010";

    public IEnumerable<Finding> Evaluate(RuleContext ctx)
    {
        var krbtgt = ctx.OfType(AdObjectType.User)
            .FirstOrDefault(u => string.Equals(u.SamAccountName, "krbtgt", StringComparison.OrdinalIgnoreCase));
        if (krbtgt is null)
        {
            yield break;
        }

        var pwdLastSet = AdAttributes.ParseFileTime(krbtgt.Value("pwdLastSet"));
        var age = AdAttributes.AgeInDays(pwdLastSet, ctx.AsOf);
        if (age is null || age < ctx.Thresholds.KrbtgtRotationDays)
        {
            yield break;
        }

        yield return RuleHelpers.Build(
            ctx,
            RuleId,
            Severity.High,
            krbtgt,
            0,
            new Dictionary<string, string>
            {
                ["ageDays"] = age.Value.ToString(),
                ["pwdLastSet"] = pwdLastSet?.ToString("u") ?? "desconhecido",
                ["thresholdDays"] = ctx.Thresholds.KrbtgtRotationDays.ToString()
            });
    }
}

/// <summary>ADCLN-GROUP-EMPTY-011 - grupo de seguranca vazio (sem membros), exceto internos.</summary>
public sealed class EmptyGroupRule : IAdHygieneRule
{
    public string RuleId => "ADCLN-GROUP-EMPTY-011";

    public IEnumerable<Finding> Evaluate(RuleContext ctx)
    {
        foreach (var group in ctx.OfType(AdObjectType.Group))
        {
            if (RuleContext.IsWellKnownBuiltIn(group) || group.Values("member").Count > 0)
            {
                continue;
            }

            // So reporta se estiver vazio ha pelo menos N dias (whenChanged como proxy).
            var reference = AdAttributes.ParseGeneralizedTime(group.Value("whenChanged") ?? group.Value("whenCreated"));
            var age = AdAttributes.AgeInDays(reference, ctx.AsOf);
            if (age is null || age < ctx.Thresholds.EmptyObjectMinDays)
            {
                continue;
            }

            yield return RuleHelpers.Build(
                ctx,
                RuleId,
                Severity.Low,
                group,
                0,
                new Dictionary<string, string>
                {
                    ["memberCount"] = "0",
                    ["ageDays"] = age.Value.ToString(),
                    ["thresholdDays"] = ctx.Thresholds.EmptyObjectMinDays.ToString(),
                    ["whenChanged"] = group.Value("whenChanged") ?? "desconhecido",
                    ["distinguishedName"] = group.DistinguishedName,
                    ["sam"] = group.SamAccountName ?? group.DistinguishedName
                });
        }
    }
}

/// <summary>ADCLN-OU-EMPTY-012 - OU sem objetos-filhos coletados.</summary>
public sealed class EmptyOuRule : IAdHygieneRule
{
    public string RuleId => "ADCLN-OU-EMPTY-012";

    public IEnumerable<Finding> Evaluate(RuleContext ctx)
    {
        foreach (var ou in ctx.OfType(AdObjectType.OrganizationalUnit))
        {
            // OU de sistema "Domain Controllers" nao deve ser removida.
            if (ou.DistinguishedName.StartsWith("OU=Domain Controllers,", StringComparison.OrdinalIgnoreCase) ||
                ctx.DnsWithChildren.Contains(ou.DistinguishedName))
            {
                continue;
            }

            var reference = AdAttributes.ParseGeneralizedTime(ou.Value("whenChanged") ?? ou.Value("whenCreated"));
            var age = AdAttributes.AgeInDays(reference, ctx.AsOf);
            if (age is null || age < ctx.Thresholds.EmptyObjectMinDays)
            {
                continue;
            }

            // Usa o DN como identificador: varias OUs podem ter o mesmo RDN (ex.: "GPO
            // Scope" em ramos diferentes), e mostrar so o nome pareceria duplicado.
            yield return RuleHelpers.Build(
                ctx,
                RuleId,
                Severity.Low,
                ou.ObjectKey,
                ou.DistinguishedName,
                0,
                new Dictionary<string, string>
                {
                    ["ageDays"] = age.Value.ToString(),
                    ["thresholdDays"] = ctx.Thresholds.EmptyObjectMinDays.ToString(),
                    ["whenCreated"] = ou.Value("whenCreated") ?? "desconhecido",
                    ["distinguishedName"] = ou.DistinguishedName
                });
        }
    }
}

/// <summary>ADCLN-GPO-UNLINKED-013 - GPO nao referenciada por nenhum gPLink.</summary>
public sealed class UnlinkedGpoRule : IAdHygieneRule
{
    public string RuleId => "ADCLN-GPO-UNLINKED-013";

    public IEnumerable<Finding> Evaluate(RuleContext ctx)
    {
        foreach (var gpo in ctx.OfType(AdObjectType.GroupPolicyContainer))
        {
            var guid = PolicyGuid(gpo);
            if (guid is null || ctx.LinkedGpoGuids.Contains(guid))
            {
                continue;
            }

            yield return GpoFinding(ctx, RuleId, Severity.Low, gpo, new Dictionary<string, string>
            {
                ["whenChanged"] = gpo.Value("whenChanged") ?? "desconhecido"
            });
        }
    }

    /// <summary>
    /// GUID de POLITICA da GPO = o {GUID} do CN do objeto (CN={GUID},CN=Policies,...).
    /// E o GUID usado em gPLink e em Get-GPO/Remove-GPO — diferente do objectGUID do objeto.
    /// </summary>
    internal static string? PolicyGuid(CollectedObject gpo)
    {
        var dn = gpo.DistinguishedName;
        var open = dn.IndexOf('{');
        var close = dn.IndexOf('}');
        return open >= 0 && close > open
            ? dn.Substring(open + 1, close - open - 1).ToLowerInvariant()
            : null;
    }

    internal static Finding GpoFinding(RuleContext ctx, string ruleId, Severity severity, CollectedObject gpo, Dictionary<string, string> evidence)
    {
        evidence["guid"] = PolicyGuid(gpo) ?? string.Empty;
        evidence["name"] = gpo.Value("displayName") ?? gpo.DistinguishedName;
        evidence["distinguishedName"] = gpo.DistinguishedName;
        return RuleHelpers.Build(ctx, ruleId, severity, gpo.ObjectKey,
            gpo.Value("displayName") ?? gpo.DistinguishedName, 0, evidence);
    }
}

/// <summary>ADCLN-GPO-EMPTY-014 - GPO sem configuracoes (versionNumber 0).</summary>
public sealed class EmptyGpoRule : IAdHygieneRule
{
    public string RuleId => "ADCLN-GPO-EMPTY-014";

    public IEnumerable<Finding> Evaluate(RuleContext ctx)
    {
        foreach (var gpo in ctx.OfType(AdObjectType.GroupPolicyContainer))
        {
            // So reporta GPO vazia se ela estiver VINCULADA: uma GPO vazia e nao
            // vinculada ja e coberta por ADCLN-GPO-UNLINKED-013 (evita duplicar).
            var guid = UnlinkedGpoRule.PolicyGuid(gpo);
            if (guid is null || !ctx.LinkedGpoGuids.Contains(guid) ||
                AdAttributes.ParseInt(gpo.Value("versionNumber")) != 0)
            {
                continue;
            }

            yield return UnlinkedGpoRule.GpoFinding(ctx, RuleId, Severity.Low, gpo, new Dictionary<string, string>
            {
                ["versionNumber"] = "0"
            });
        }
    }
}

/// <summary>ADCLN-GPO-DISABLED-015 - GPO com ambas as secoes desabilitadas (flags == 3).</summary>
public sealed class DisabledGpoRule : IAdHygieneRule
{
    public string RuleId => "ADCLN-GPO-DISABLED-015";

    public IEnumerable<Finding> Evaluate(RuleContext ctx)
    {
        foreach (var gpo in ctx.OfType(AdObjectType.GroupPolicyContainer))
        {
            // Idem: so faz sentido para GPO vinculada (a nao vinculada ja e reportada).
            var guid = UnlinkedGpoRule.PolicyGuid(gpo);
            if (guid is null || !ctx.LinkedGpoGuids.Contains(guid) ||
                AdAttributes.ParseInt(gpo.Value("flags")) != 3)
            {
                continue;
            }

            yield return UnlinkedGpoRule.GpoFinding(ctx, RuleId, Severity.Low, gpo, new Dictionary<string, string>
            {
                ["flags"] = "3"
            });
        }
    }
}

/// <summary>ADHARD-PWDNOTREQD-016 - conta que dispensa senha (PASSWD_NOTREQD).</summary>
public sealed class PasswordNotRequiredRule : IAdHygieneRule
{
    public string RuleId => "ADHARD-PWDNOTREQD-016";

    public IEnumerable<Finding> Evaluate(RuleContext ctx)
    {
        foreach (var user in ctx.OfType(AdObjectType.User))
        {
            if (RuleContext.IsWellKnownBuiltIn(user) ||
                !AdAttributes.HasFlag(user.Value("userAccountControl"), UserAccountControlFlags.PasswordNotRequired))
            {
                continue;
            }

            yield return RuleHelpers.Build(ctx, RuleId, Severity.High, user, 0, new Dictionary<string, string>
            {
                ["distinguishedName"] = user.DistinguishedName,
                ["sam"] = user.SamAccountName ?? user.DistinguishedName
            });
        }
    }
}

/// <summary>ADHARD-PWDNOEXPIRE-017 - conta habilitada com senha que nunca expira.</summary>
public sealed class PasswordNeverExpiresRule : IAdHygieneRule
{
    public string RuleId => "ADHARD-PWDNOEXPIRE-017";

    public IEnumerable<Finding> Evaluate(RuleContext ctx)
    {
        foreach (var user in ctx.OfType(AdObjectType.User))
        {
            // Contas gerenciadas (gMSA/sMSA/maquina, sAMAccountName terminando em '$')
            // tem a senha rotacionada pelo proprio AD -> nao reportar.
            if (RuleContext.IsWellKnownBuiltIn(user) ||
                RuleContext.IsManagedAccount(user) ||
                AdAttributes.IsEnabled(user.Value("userAccountControl")) != true ||
                !AdAttributes.HasFlag(user.Value("userAccountControl"), UserAccountControlFlags.DontExpirePassword))
            {
                continue;
            }

            yield return RuleHelpers.Build(ctx, RuleId, Severity.Medium, user, 0, new Dictionary<string, string>
            {
                ["distinguishedName"] = user.DistinguishedName,
                ["sam"] = user.SamAccountName ?? user.DistinguishedName
            });
        }
    }
}

/// <summary>ADHARD-REVERSIBLEPWD-018 - criptografia reversivel de senha habilitada.</summary>
public sealed class ReversiblePasswordRule : IAdHygieneRule
{
    public string RuleId => "ADHARD-REVERSIBLEPWD-018";

    public IEnumerable<Finding> Evaluate(RuleContext ctx)
    {
        foreach (var user in ctx.OfType(AdObjectType.User))
        {
            if (RuleContext.IsWellKnownBuiltIn(user) ||
                !AdAttributes.HasFlag(user.Value("userAccountControl"), UserAccountControlFlags.EncryptedTextPasswordAllowed))
            {
                continue;
            }

            yield return RuleHelpers.Build(ctx, RuleId, Severity.High, user, 0, new Dictionary<string, string>
            {
                ["distinguishedName"] = user.DistinguishedName,
                ["sam"] = user.SamAccountName ?? user.DistinguishedName
            });
        }
    }
}

/// <summary>ADPRV-KERBEROAST-019 - conta de usuario com SPN (kerberoastable).</summary>
public sealed class KerberoastRule : IAdHygieneRule
{
    public string RuleId => "ADPRV-KERBEROAST-019";

    public IEnumerable<Finding> Evaluate(RuleContext ctx)
    {
        foreach (var user in ctx.OfType(AdObjectType.User))
        {
            // gMSA (gerenciada pelo AD) e a propria recomendacao contra kerberoast -> nao reportar.
            if (RuleContext.IsWellKnownBuiltIn(user) ||
                RuleContext.IsManagedAccount(user) ||
                user.Values("servicePrincipalName").Count == 0)
            {
                continue;
            }

            var privileged = ctx.IsPrivileged(user);
            yield return RuleHelpers.Build(
                ctx,
                RuleId,
                privileged ? Severity.Critical : Severity.High,
                user,
                privileged ? ctx.Criticality.Tier0Multiplier : 0,
                new Dictionary<string, string>
                {
                    ["privileged"] = privileged ? "true" : "false",
                    ["servicePrincipalName"] = string.Join(", ", user.Values("servicePrincipalName")),
                    ["distinguishedName"] = user.DistinguishedName,
                    ["sam"] = user.SamAccountName ?? user.DistinguishedName
                });
        }
    }
}

/// <summary>ADCLN-COMP-LEGACYOS-020 - computador habilitado com SO sem suporte.</summary>
public sealed class LegacyOsComputerRule : IAdHygieneRule
{
    public string RuleId => "ADCLN-COMP-LEGACYOS-020";

    // Apenas SOs inequivocamente fora de suporte (sem ambiguidade de LTSC/ESU):
    // Win10 LTSC vai ate 2029-2032, por isso "Windows 10/11" e Server 2016+ NAO entram.
    // Ref. Microsoft Lifecycle (Server 2012/2012 R2 fim em 10/2023; pre-2016 e pre-Win10 ja encerrados).
    private static readonly string[] LegacyMarkers =
    [
        "Windows 2000", "Windows XP", "Windows Vista", "Windows 7", "Windows 8",
        "Server 2003", "Server 2008", "Server 2012"
    ];

    public IEnumerable<Finding> Evaluate(RuleContext ctx)
    {
        foreach (var computer in ctx.OfType(AdObjectType.Computer))
        {
            if (AdAttributes.IsEnabled(computer.Value("userAccountControl")) != true)
            {
                continue;
            }

            var os = computer.Value("operatingSystem");
            if (string.IsNullOrWhiteSpace(os) ||
                !LegacyMarkers.Any(m => os.Contains(m, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            yield return RuleHelpers.Build(ctx, RuleId, Severity.Medium, computer, 0, new Dictionary<string, string>
            {
                ["operatingSystem"] = os,
                ["operatingSystemVersion"] = computer.Value("operatingSystemVersion") ?? "desconhecido",
                ["distinguishedName"] = computer.DistinguishedName,
                ["sam"] = computer.SamAccountName ?? computer.DistinguishedName
            });
        }
    }
}

/// <summary>ADPRV-ADMINCOUNT-ORPHAN-021 - adminCount=1 sem pertencer a grupo privilegiado.</summary>
public sealed class OrphanAdminCountRule : IAdHygieneRule
{
    public string RuleId => "ADPRV-ADMINCOUNT-ORPHAN-021";

    public IEnumerable<Finding> Evaluate(RuleContext ctx)
    {
        foreach (var obj in ctx.Objects)
        {
            if (obj.ObjectType is not (AdObjectType.User or AdObjectType.Group) ||
                RuleContext.IsWellKnownBuiltIn(obj) ||
                AdAttributes.ParseInt(obj.Value("adminCount")) != 1 ||
                ctx.PrivilegedMembership.ContainsKey(obj.DistinguishedName))
            {
                continue;
            }

            yield return RuleHelpers.Build(ctx, RuleId, Severity.Medium, obj, 0, new Dictionary<string, string>
            {
                ["objectType"] = obj.ObjectType.ToString(),
                ["adminCount"] = "1",
                ["distinguishedName"] = obj.DistinguishedName,
                ["sam"] = obj.SamAccountName ?? obj.DistinguishedName
            });
        }
    }
}

/// <summary>ADGOV-CONFLICT-022 - objetos de conflito de replicacao (CNF).</summary>
public sealed class ConflictObjectsRule : IAdHygieneRule
{
    public string RuleId => "ADGOV-CONFLICT-022";

    public IEnumerable<Finding> Evaluate(RuleContext ctx)
    {
        var domain = ctx.DomainRoot;
        var count = AdAttributes.ParseInt(domain?.Value("conflictObjectCount"));
        if (domain is null || count is null or 0)
        {
            yield break;
        }

        yield return RuleHelpers.Build(ctx, RuleId, Severity.Medium, domain.ObjectKey, ctx.DomainName, 0,
            new Dictionary<string, string>
            {
                ["count"] = count.Value.ToString(),
                ["sample"] = domain.Value("conflictObjectSample") ?? string.Empty,
                ["domain"] = ctx.DomainName
            });
    }
}

/// <summary>ADGOV-LOSTFOUND-023 - objetos orfaos em LostAndFound.</summary>
public sealed class LostAndFoundRule : IAdHygieneRule
{
    public string RuleId => "ADGOV-LOSTFOUND-023";

    public IEnumerable<Finding> Evaluate(RuleContext ctx)
    {
        var domain = ctx.DomainRoot;
        var count = AdAttributes.ParseInt(domain?.Value("lostFoundCount"));
        if (domain is null || count is null or 0)
        {
            yield break;
        }

        yield return RuleHelpers.Build(ctx, RuleId, Severity.Medium, domain.ObjectKey, ctx.DomainName, 0,
            new Dictionary<string, string>
            {
                ["count"] = count.Value.ToString(),
                ["sample"] = domain.Value("lostFoundSample") ?? string.Empty,
                ["domain"] = ctx.DomainName
            });
    }
}
