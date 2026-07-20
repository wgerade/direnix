using Direnix.Core.Findings;
using Direnix.Core.Rules;
using Xunit;

namespace Direnix.Core.Tests;

public class RiskScoringTests
{
    [Theory]
    [InlineData(Severity.Critical, 90)]
    [InlineData(Severity.High, 70)]
    [InlineData(Severity.Medium, 40)]
    [InlineData(Severity.Low, 10)]
    [InlineData(Severity.Info, 0)]
    public void TechnicalBase_MatchesBands(Severity severity, int expected) =>
        Assert.Equal(expected, RiskScoring.TechnicalBase(severity));

    [Fact]
    public void Compute_ClampsToHundred() =>
        Assert.Equal(100, RiskScoring.Compute(Severity.High, businessMultipliers: 50));

    [Fact]
    public void Compute_AppliesCompensatingControls() =>
        Assert.Equal(55, RiskScoring.Compute(Severity.High, compensating: 15));

    [Theory]
    [InlineData(95, Severity.Critical)]
    [InlineData(72, Severity.High)]
    [InlineData(40, Severity.Medium)]
    [InlineData(9, Severity.Info)]
    public void SeverityFromScore_MapsBands(int score, Severity expected) =>
        Assert.Equal(expected, RiskScoring.SeverityFromScore(score));

    [Fact]
    public void DeriveDomainName_FromDistinguishedName() =>
        Assert.Equal("corp.local", HygieneRuleEngine.DeriveDomainName("DC=corp,DC=local"));
}
