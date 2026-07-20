namespace Direnix.Core.Scheduling;

public enum ScheduleFrequency
{
    Daily,
    Weekly,
    IntervalHours
}

/// <summary>
/// Configuração da coleta automática. Não guarda credencial: a coleta roda sob a
/// identidade do serviço (gMSA) via Kerberos/Negotiate.
/// </summary>
public sealed record ScheduleConfig(
    bool Enabled,
    ScheduleFrequency Frequency,
    string TimeOfDay,            // "HH:mm" (hora local) para Daily/Weekly
    IReadOnlyList<int> Weekdays, // 0=domingo .. 6=sábado, para Weekly
    int IntervalHours,           // para IntervalHours
    string Host,
    string Protocol,             // sempre "Ldaps"
    string? ProfileName,
    DateTimeOffset? LastRunAt,
    DateTimeOffset? NextRunAt)
{
    public static ScheduleConfig Disabled() =>
        new(false, ScheduleFrequency.Daily, "02:00", Array.Empty<int>(), 24, "", "Ldaps", null, null, null);
}
