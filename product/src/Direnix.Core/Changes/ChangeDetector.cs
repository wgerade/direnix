using Direnix.Core.Collection;
using Direnix.Core.Findings;

namespace Direnix.Core.Changes;

/// <summary>
/// Compara o estado anterior de um objeto (atributos da última coleta) com o estado
/// atual e produz mudanças relevantes. Lógica pura e testável; created/deleted são
/// tratados na camada de persistência (presença/ausência de estado anterior).
/// Mantém um conjunto branco de atributos para não inundar a Timeline de ruído.
/// </summary>
public static class ChangeDetector
{
    private static readonly int[] PrivilegedRids = [512, 518, 519, 520, 544, 548, 549, 550, 551];

    private static readonly (UserAccountControlFlags Flag, string Name)[] DangerousFlags =
    [
        (UserAccountControlFlags.DontRequirePreauth, "DontRequirePreauth"),
        (UserAccountControlFlags.TrustedForDelegation, "TrustedForDelegation"),
        (UserAccountControlFlags.PasswordNotRequired, "PasswordNotRequired"),
        (UserAccountControlFlags.EncryptedTextPasswordAllowed, "EncryptedTextPasswordAllowed")
    ];

    private static readonly string[] WhitelistAttributes =
    [
        "description", "manager", "userPrincipalName", "operatingSystem", "displayName"
    ];

    public static Severity SeverityFor(ChangeType type) => type switch
    {
        ChangeType.PrivilegedMemberAdded => Severity.Critical,
        ChangeType.DangerousFlagSet => Severity.High,
        ChangeType.ObjectDeleted => Severity.Medium,
        ChangeType.AdminCountChanged => Severity.Medium,
        ChangeType.SpnAdded => Severity.Medium,
        ChangeType.PrivilegedMemberRemoved => Severity.Medium,
        ChangeType.AccountEnabled => Severity.Low,
        ChangeType.GpoLinkChanged => Severity.Low,
        _ => Severity.Info
    };

    /// <summary>
    /// Detecta mudanças entre <paramref name="previous"/> (atributos da última coleta)
    /// e <paramref name="current"/> (objeto recém-coletado).
    /// </summary>
    public static IEnumerable<DetectedChange> Detect(
        CollectedObject current,
        IReadOnlyDictionary<string, IReadOnlyList<string>> previous)
    {
        var changes = new List<DetectedChange>();

        // Membros de grupo (added/removed), com escalada para grupo privilegiado.
        if (current.ObjectType == AdObjectType.Group)
        {
            var privileged = IsPrivilegedGroup(current);
            var before = Multi(previous, "member");
            var after = current.Values("member").ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var added in after.Where(m => !before.Contains(m)))
            {
                changes.Add(new DetectedChange(
                    privileged ? ChangeType.PrivilegedMemberAdded : ChangeType.MemberAdded,
                    "member", null, added,
                    SeverityFor(privileged ? ChangeType.PrivilegedMemberAdded : ChangeType.MemberAdded)));
            }

            foreach (var removed in before.Where(m => !after.Contains(m)))
            {
                changes.Add(new DetectedChange(
                    privileged ? ChangeType.PrivilegedMemberRemoved : ChangeType.MemberRemoved,
                    "member", removed, null,
                    SeverityFor(privileged ? ChangeType.PrivilegedMemberRemoved : ChangeType.MemberRemoved)));
            }
        }

        // userAccountControl: habilitar/desabilitar e flags perigosas.
        var prevUacRaw = First(previous, "userAccountControl");
        var curUacRaw = current.Value("userAccountControl");
        if (prevUacRaw is not null && curUacRaw is not null && prevUacRaw != curUacRaw)
        {
            var prevUac = AdAttributes.ParseUac(prevUacRaw);
            var curUac = AdAttributes.ParseUac(curUacRaw);

            var wasDisabled = prevUac.HasFlag(UserAccountControlFlags.AccountDisabled);
            var isDisabled = curUac.HasFlag(UserAccountControlFlags.AccountDisabled);
            if (wasDisabled != isDisabled)
            {
                var type = isDisabled ? ChangeType.AccountDisabled : ChangeType.AccountEnabled;
                changes.Add(new DetectedChange(type, "userAccountControl", prevUacRaw, curUacRaw, SeverityFor(type)));
            }

            foreach (var (flag, name) in DangerousFlags)
            {
                var had = prevUac.HasFlag(flag);
                var has = curUac.HasFlag(flag);
                if (had != has)
                {
                    var type = has ? ChangeType.DangerousFlagSet : ChangeType.DangerousFlagCleared;
                    changes.Add(new DetectedChange(type, name, had ? "set" : "clear", has ? "set" : "clear", SeverityFor(type)));
                }
            }
        }

        // adminCount.
        var prevAdmin = First(previous, "adminCount");
        var curAdmin = current.Value("adminCount");
        if ((prevAdmin ?? "0") != (curAdmin ?? "0"))
        {
            changes.Add(new DetectedChange(ChangeType.AdminCountChanged, "adminCount", prevAdmin, curAdmin,
                SeverityFor(ChangeType.AdminCountChanged)));
        }

        // SPN adicionado (superfície kerberoast).
        var beforeSpn = Multi(previous, "servicePrincipalName");
        foreach (var spn in current.Values("servicePrincipalName").Where(s => !beforeSpn.Contains(s)))
        {
            changes.Add(new DetectedChange(ChangeType.SpnAdded, "servicePrincipalName", null, spn,
                SeverityFor(ChangeType.SpnAdded)));
        }

        // gPLink (vínculo de GPO).
        var prevLink = First(previous, "gPLink");
        var curLink = current.Value("gPLink");
        if ((prevLink ?? "") != (curLink ?? ""))
        {
            changes.Add(new DetectedChange(ChangeType.GpoLinkChanged, "gPLink", prevLink, curLink,
                SeverityFor(ChangeType.GpoLinkChanged)));
        }

        // Atributos de whitelist (genérico, Info).
        foreach (var attr in WhitelistAttributes)
        {
            var before = First(previous, attr);
            var after = current.Value(attr);
            if ((before ?? "") != (after ?? ""))
            {
                changes.Add(new DetectedChange(ChangeType.AttributeChanged, attr, before, after,
                    SeverityFor(ChangeType.AttributeChanged)));
            }
        }

        return changes;
    }

    private static bool IsPrivilegedGroup(CollectedObject group)
    {
        var rid = AdAttributes.RelativeId(group.ObjectSid);
        return rid is not null && PrivilegedRids.Contains(rid.Value);
    }

    private static HashSet<string> Multi(IReadOnlyDictionary<string, IReadOnlyList<string>> attrs, string key) =>
        attrs.TryGetValue(key, out var values)
            ? values.ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private static string? First(IReadOnlyDictionary<string, IReadOnlyList<string>> attrs, string key) =>
        attrs.TryGetValue(key, out var values) && values.Count > 0 ? values[0] : null;
}
