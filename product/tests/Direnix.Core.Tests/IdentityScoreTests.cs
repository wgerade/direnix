using Direnix.Core.Findings;
using Direnix.Core.Scoring;
using Xunit;

namespace Direnix.Core.Tests;

public class IdentityScoreTests
{
    [Fact]
    public void NoFindings_IsPerfectAndGreen()
    {
        var r = IdentityScore.Compute(Array.Empty<ScoreInput>());
        Assert.Equal(100, r.Score);
        Assert.Equal(100, r.Tier0Score);
        Assert.Equal("green", r.Health);
    }

    [Fact]
    public void CriticalsDrivesScoreDownAndRed()
    {
        var r = IdentityScore.Compute(new[]
        {
            new ScoreInput(Severity.Critical, true),
            new ScoreInput(Severity.Critical, false),
            new ScoreInput(Severity.High, false)
        });
        // 100 - (25 + 25 + 10) = 40
        Assert.Equal(40, r.Score);
        Assert.Equal("red", r.Health);
        // Tier0 so o critico privilegiado: 100 - 25 = 75
        Assert.Equal(75, r.Tier0Score);
    }

    [Fact]
    public void ScoreNeverGoesNegative()
    {
        var many = Enumerable.Range(0, 20).Select(_ => new ScoreInput(Severity.Critical, false));
        Assert.Equal(0, IdentityScore.Compute(many).Score);
    }

    [Fact]
    public void Health_Yellow_Boundary()
    {
        // 3 mediums = -12 -> 88 (yellow)
        var r = IdentityScore.Compute(new[]
        {
            new ScoreInput(Severity.Medium, false),
            new ScoreInput(Severity.Medium, false),
            new ScoreInput(Severity.Medium, false)
        });
        Assert.Equal(88, r.Score);
        Assert.Equal("yellow", r.Health);
    }
}
