using Direnix.Core.Collection;
using Direnix.Core.Rules;

namespace Direnix.Core.Indicators;

/// <summary>
/// Avalia os indicadores operacionais sobre um run coletado. Puro/testavel: nao
/// toca rede nem banco. Built-ins operam sobre os objetos ja coletados; os
/// customizados vêm ja resolvidos em <see cref="CollectionResult.CustomIndicatorMatches"/>.
/// </summary>
public static class IndicatorEngine
{
    private const long NeverExpires = long.MaxValue;

    public static IReadOnlyList<IndicatorResult> Evaluate(CollectionResult result, RuleProfile profile)
    {
        var asOf = result.CompletedAt;
        var horizon = profile.IndicatorHorizonDays <= 0 ? 7 : profile.IndicatorHorizonDays;
        var users = result.Objects.Where(o => o.ObjectType == AdObjectType.User).ToList();
        var domain = result.Objects.FirstOrDefault(o => o.ObjectType == AdObjectType.Domain);
        var lockoutDurationTicks = AbsIntervalTicks(domain?.Value("lockoutDuration"));

        var results = new List<IndicatorResult>();

        void AddBuiltIn(string id, string title, string category, IEnumerable<IndicatorItem> items)
        {
            if (!profile.IsIndicatorEnabled(id))
            {
                return;
            }

            var list = items.ToList();
            results.Add(new IndicatorResult(id, title, category, false, list.Count,
                list.Take(IndicatorCatalog.MaxItems).ToList()));
        }

        // Senhas vencendo / expiradas.
        var expiringItems = new List<IndicatorItem>();
        var expiredItems = new List<IndicatorItem>();
        foreach (var user in users)
        {
            if (!IsEligiblePasswordAccount(user))
            {
                continue;
            }

            var expiry = ParsePasswordExpiry(user.Value("msDS-UserPasswordExpiryTimeComputed"));
            if (expiry is null)
            {
                continue; // nunca expira, desconhecido ou nao aplicavel
            }

            if (expiry.Value <= asOf)
            {
                expiredItems.Add(ToItem(user, DetailDate("Expirou", expiry.Value)));
            }
            else if (expiry.Value <= asOf.AddDays(horizon))
            {
                var today = expiry.Value.Date == asOf.Date ? " (hoje)" : string.Empty;
                expiringItems.Add(ToItem(user, $"Vence {expiry.Value:yyyy-MM-dd}{today}"));
            }
        }

        AddBuiltIn(IndicatorCatalog.PasswordExpiring, $"Senhas vencendo (≤{horizon}d)", "Senha", expiringItems);
        AddBuiltIn(IndicatorCatalog.PasswordExpired, "Senhas expiradas", "Senha", expiredItems);

        // Contas bloqueadas.
        var lockedItems = new List<IndicatorItem>();
        foreach (var user in users)
        {
            if (RuleContext.IsWellKnownBuiltIn(user))
            {
                continue;
            }

            var lockout = AdAttributes.ParseFileTime(user.Value("lockoutTime"));
            if (lockout is null)
            {
                continue; // lockoutTime = 0 -> nao bloqueada
            }

            // Bloqueio expira sozinho apos lockoutDuration; se ja passou, ignora.
            // lockoutDuration = 0 no dominio => bloqueio ate desbloqueio manual.
            if (lockoutDurationTicks > 0 && lockout.Value.AddTicks(lockoutDurationTicks) <= asOf)
            {
                continue;
            }

            lockedItems.Add(ToItem(user, DetailDate("Bloqueada", lockout.Value)));
        }

        AddBuiltIn(IndicatorCatalog.AccountLocked, "Contas bloqueadas", "Conta", lockedItems);

        // Contas a expirar (accountExpires no horizonte).
        var acctExpiringItems = new List<IndicatorItem>();
        foreach (var user in users)
        {
            if (RuleContext.IsWellKnownBuiltIn(user) ||
                AdAttributes.IsEnabled(user.Value("userAccountControl")) != true)
            {
                continue;
            }

            var expires = ParseAccountExpires(user.Value("accountExpires"));
            if (expires is null || expires.Value <= asOf || expires.Value > asOf.AddDays(horizon))
            {
                continue;
            }

            acctExpiringItems.Add(ToItem(user, $"Expira {expires.Value:yyyy-MM-dd}"));
        }

        AddBuiltIn(IndicatorCatalog.AccountExpiring, $"Contas a expirar (≤{horizon}d)", "Conta", acctExpiringItems);

        // Indicadores customizados (ja resolvidos pelo coletor).
        foreach (var match in result.CustomIndicatorMatches)
        {
            if (!profile.CustomIndicators.Any(c => string.Equals(c.Id, match.IndicatorId, StringComparison.OrdinalIgnoreCase) && c.Enabled))
            {
                continue;
            }

            var items = match.Objects
                .Select(o => new IndicatorItem(o.DisplayName, o.DistinguishedName, o.ObjectSid,
                    o.Value("displayName")))
                .ToList();
            results.Add(new IndicatorResult(match.IndicatorId, match.Name, "Custom", true,
                items.Count, items.Take(IndicatorCatalog.MaxItems).ToList()));
        }

        return results;
    }

    private static bool IsEligiblePasswordAccount(CollectedObject user) =>
        !RuleContext.IsWellKnownBuiltIn(user) &&
        !RuleContext.IsManagedAccount(user) &&
        AdAttributes.IsEnabled(user.Value("userAccountControl")) == true &&
        !AdAttributes.HasFlag(user.Value("userAccountControl"), UserAccountControlFlags.DontExpirePassword);

    /// <summary>
    /// Interpreta msDS-UserPasswordExpiryTimeComputed: Int64.Max = nunca expira
    /// (null); 0 = trocar no proximo logon (retorna epoch p/ contar como expirada);
    /// caso contrario, o FILETIME convertido.
    /// </summary>
    internal static DateTimeOffset? ParsePasswordExpiry(string? raw)
    {
        var ticks = AdAttributes.ParseLong(raw);
        if (ticks is null || ticks == NeverExpires)
        {
            return null;
        }

        if (ticks == 0)
        {
            return DateTimeOffset.UnixEpoch; // forca contagem como expirada
        }

        try
        {
            return DateTimeOffset.FromFileTime(ticks.Value);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    /// <summary>accountExpires: 0 ou Int64.Max = nunca; caso contrario FILETIME.</summary>
    internal static DateTimeOffset? ParseAccountExpires(string? raw)
    {
        var ticks = AdAttributes.ParseLong(raw);
        if (ticks is null or 0 || ticks == NeverExpires)
        {
            return null;
        }

        try
        {
            return DateTimeOffset.FromFileTime(ticks.Value);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    /// <summary>Valor absoluto (em ticks) de um intervalo AD negativo como lockoutDuration.</summary>
    private static long AbsIntervalTicks(string? raw)
    {
        var value = AdAttributes.ParseLong(raw);
        return value is null ? 0 : Math.Abs(value.Value);
    }

    private static IndicatorItem ToItem(CollectedObject obj, string? detail) =>
        new(obj.DisplayName, obj.DistinguishedName, obj.ObjectSid, detail);

    private static string DetailDate(string label, DateTimeOffset when) =>
        $"{label} {when:yyyy-MM-dd HH:mm}";
}
