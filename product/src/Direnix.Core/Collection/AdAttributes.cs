using System.Globalization;

namespace Direnix.Core.Collection;

/// <summary>
/// Flags relevantes de `userAccountControl`.
/// Referencia: Microsoft AD UAC flags.
/// </summary>
[Flags]
public enum UserAccountControlFlags
{
    None = 0,
    AccountDisabled = 0x0002,
    PasswordNotRequired = 0x0020,
    EncryptedTextPasswordAllowed = 0x0080,
    WorkstationTrustAccount = 0x1000,
    ServerTrustAccount = 0x2000,
    DontExpirePassword = 0x10000,
    TrustedForDelegation = 0x80000,
    NotDelegated = 0x100000,
    UseDesKeyOnly = 0x200000,
    DontRequirePreauth = 0x400000
}

/// <summary>
/// Helpers de parsing de atributos LDAP brutos para tipos usados pelas regras.
/// </summary>
public static class AdAttributes
{
    public static int? ParseInt(string? raw) =>
        int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;

    public static long? ParseLong(string? raw) =>
        long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;

    public static UserAccountControlFlags ParseUac(string? raw)
    {
        var value = ParseInt(raw);
        return value is null ? UserAccountControlFlags.None : (UserAccountControlFlags)value.Value;
    }

    public static bool HasFlag(string? uacRaw, UserAccountControlFlags flag) =>
        ParseUac(uacRaw).HasFlag(flag);

    public static bool? IsEnabled(string? uacRaw)
    {
        var value = ParseInt(uacRaw);
        if (value is null)
        {
            return null;
        }

        return !((UserAccountControlFlags)value.Value).HasFlag(UserAccountControlFlags.AccountDisabled);
    }

    /// <summary>
    /// Converte atributos FILETIME do AD (Int64, intervalos de 100ns desde 1601 UTC),
    /// como `lastLogonTimestamp` e `pwdLastSet`. Retorna null para 0 (nunca) ou
    /// 0x7FFFFFFFFFFFFFFF (nunca expira).
    /// </summary>
    public static DateTimeOffset? ParseFileTime(string? raw)
    {
        var ticks = ParseLong(raw);
        if (ticks is null or 0 || ticks == long.MaxValue)
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

    /// <summary>
    /// Converte `whenCreated`/`whenChanged` no formato GeneralizedTime
    /// (`yyyyMMddHHmmss.0Z`).
    /// </summary>
    public static DateTimeOffset? ParseGeneralizedTime(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var trimmed = raw.Length >= 14 ? raw[..14] : raw;
        if (DateTime.TryParseExact(
                trimmed,
                "yyyyMMddHHmmss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
        {
            return new DateTimeOffset(parsed, TimeSpan.Zero);
        }

        return null;
    }

    /// <summary>
    /// Idade em dias desde um timestamp ate o instante de referencia.
    /// </summary>
    public static int? AgeInDays(DateTimeOffset? timestamp, DateTimeOffset asOf) =>
        timestamp is null ? null : (int)Math.Max(0, (asOf - timestamp.Value).TotalDays);

    /// <summary>
    /// Extrai o RID final de um SID (`S-1-5-21-...-512` -> 512).
    /// </summary>
    public static int? RelativeId(string? sid)
    {
        if (string.IsNullOrWhiteSpace(sid))
        {
            return null;
        }

        var lastDash = sid.LastIndexOf('-');
        if (lastDash < 0 || lastDash == sid.Length - 1)
        {
            return null;
        }

        return ParseInt(sid[(lastDash + 1)..]);
    }
}
