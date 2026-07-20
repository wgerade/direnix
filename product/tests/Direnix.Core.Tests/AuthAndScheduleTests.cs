using Direnix.Core.Auth;
using Direnix.Core.Scheduling;
using Xunit;

namespace Direnix.Core.Tests;

public class AuthAndScheduleTests
{
    [Fact]
    public void PasswordHasher_VerifiesCorrectPassword()
    {
        var h = PasswordHasher.Hash("Sup3rSenha!", iterations: 50_000);
        Assert.NotEqual("Sup3rSenha!", h.Hash);
        Assert.True(PasswordHasher.Verify("Sup3rSenha!", h.Hash, h.Salt, h.Iterations));
        Assert.False(PasswordHasher.Verify("errada", h.Hash, h.Salt, h.Iterations));
    }

    [Fact]
    public void PasswordHasher_UsesRandomSalt()
    {
        var a = PasswordHasher.Hash("mesma", iterations: 50_000);
        var b = PasswordHasher.Hash("mesma", iterations: 50_000);
        Assert.NotEqual(a.Salt, b.Salt);
        Assert.NotEqual(a.Hash, b.Hash);
    }

    private static ScheduleConfig Cfg(ScheduleFrequency freq, string time = "02:00", int interval = 24, params int[] weekdays) =>
        new(true, freq, time, weekdays, interval, "dc01", "Ldaps", "MicrosoftDefault", null, null);

    [Fact]
    public void Schedule_DisabledHasNoNextRun() =>
        Assert.Null(ScheduleCalculator.Next(ScheduleConfig.Disabled(), DateTimeOffset.Now));

    [Fact]
    public void Schedule_IntervalAddsHours()
    {
        var from = new DateTimeOffset(2026, 6, 26, 10, 0, 0, TimeSpan.Zero);
        Assert.Equal(from.AddHours(6), ScheduleCalculator.Next(Cfg(ScheduleFrequency.IntervalHours, interval: 6), from));
    }

    [Fact]
    public void Schedule_DailyPicksTodayThenTomorrow()
    {
        var morning = new DateTimeOffset(2026, 6, 26, 1, 0, 0, TimeSpan.Zero);
        var next = ScheduleCalculator.Next(Cfg(ScheduleFrequency.Daily, "02:00"), morning);
        Assert.Equal(new DateTimeOffset(2026, 6, 26, 2, 0, 0, TimeSpan.Zero), next);

        var afternoon = new DateTimeOffset(2026, 6, 26, 5, 0, 0, TimeSpan.Zero);
        var nextDay = ScheduleCalculator.Next(Cfg(ScheduleFrequency.Daily, "02:00"), afternoon);
        Assert.Equal(new DateTimeOffset(2026, 6, 27, 2, 0, 0, TimeSpan.Zero), nextDay);
    }

    [Fact]
    public void Schedule_WeeklyFindsNextConfiguredDay()
    {
        // 2026-06-26 é uma sexta-feira (DayOfWeek.Friday = 5). Próxima segunda (1).
        var friday = new DateTimeOffset(2026, 6, 26, 10, 0, 0, TimeSpan.Zero);
        var next = ScheduleCalculator.Next(Cfg(ScheduleFrequency.Weekly, "03:00", 24, 1), friday);
        Assert.Equal(DayOfWeek.Monday, next!.Value.DayOfWeek);
        Assert.Equal(3, next.Value.Hour);
    }
}
