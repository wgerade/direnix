using System.Globalization;

namespace Direnix.Core.Scheduling;

/// <summary>
/// Calcula a próxima execução de um agendamento a partir de um instante de
/// referência. Lógica pura/testável; horas de dia são interpretadas no fuso local
/// do <paramref name="from"/>.
/// </summary>
public static class ScheduleCalculator
{
    public static DateTimeOffset? Next(ScheduleConfig config, DateTimeOffset from)
    {
        if (!config.Enabled)
        {
            return null;
        }

        switch (config.Frequency)
        {
            case ScheduleFrequency.IntervalHours:
                return from.AddHours(Math.Max(1, config.IntervalHours));

            case ScheduleFrequency.Daily:
            {
                var time = ParseTime(config.TimeOfDay);
                var candidate = OnDate(from, time);
                return candidate > from ? candidate : candidate.AddDays(1);
            }

            case ScheduleFrequency.Weekly:
            {
                var time = ParseTime(config.TimeOfDay);
                var days = (config.Weekdays is { Count: > 0 } ? config.Weekdays : new[] { (int)from.DayOfWeek })
                    .Distinct().OrderBy(d => d).ToList();

                for (var offset = 0; offset <= 7; offset++)
                {
                    var day = from.AddDays(offset);
                    if (!days.Contains((int)day.DayOfWeek))
                    {
                        continue;
                    }

                    var candidate = OnDate(day, time);
                    if (candidate > from)
                    {
                        return candidate;
                    }
                }

                return OnDate(from.AddDays(7), time);
            }

            default:
                return null;
        }
    }

    private static TimeSpan ParseTime(string hhmm)
    {
        if (TimeSpan.TryParseExact(hhmm, "hh\\:mm", CultureInfo.InvariantCulture, out var t))
        {
            return t;
        }
        return new TimeSpan(2, 0, 0);
    }

    private static DateTimeOffset OnDate(DateTimeOffset reference, TimeSpan time) =>
        new(reference.Year, reference.Month, reference.Day, time.Hours, time.Minutes, 0, reference.Offset);
}
