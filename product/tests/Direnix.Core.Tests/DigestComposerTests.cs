using Direnix.Core.Notifications;
using Direnix.Core.Reporting;
using Direnix.Core.Storage;
using Xunit;

namespace Direnix.Core.Tests;

public class DigestComposerTests
{
    private static ReportModel Model(
        int newFindings = 0,
        int changeCount = 0,
        int indicatorCount = 0,
        string? domain = "corp.exemplo.local") =>
        new(
            GeneratedAt: DateTimeOffset.UtcNow,
            ProductVersion: "0.9.0",
            PortalUrl: "http://127.0.0.1:8787/",
            DomainName: domain,
            CoverageMode: "Full",
            LatestRunCompletedAt: DateTimeOffset.UtcNow,
            IdentityScore: 71,
            Tier0Score: 64,
            ActiveFindings: 15,
            SeverityBreakdown: Array.Empty<SeverityCount>(),
            CategoryBreakdown: Array.Empty<CategoryCount>(),
            TopFindings: Array.Empty<FindingRow>(),
            Indicators: indicatorCount > 0
                ? new[] { new IndicatorResultRow("pw-expiring", "Senhas vencendo", "Password", false, indicatorCount, Array.Empty<Direnix.Core.Indicators.IndicatorItem>()) }
                : Array.Empty<IndicatorResultRow>(),
            ChangeSummary24H: changeCount > 0
                ? new[] { new ChangeCount("MemberAdded", changeCount) }
                : Array.Empty<ChangeCount>(),
            NewFindings24H: newFindings,
            Inventory: Array.Empty<InventoryState>());

    [Fact]
    public void OnlyWhenActivity_NoActivity_ReturnsNull()
    {
        var message = DigestComposer.Compose(Model(), DigestPolicy.OnlyWhenActivity, "pt");
        Assert.Null(message);
    }

    [Fact]
    public void OnlyWhenActivity_WithNewFindings_Sends()
    {
        var message = DigestComposer.Compose(Model(newFindings: 3), DigestPolicy.OnlyWhenActivity, "pt");
        Assert.NotNull(message);
        Assert.Contains("corp.exemplo.local", message!.Subject);
    }

    [Fact]
    public void OnlyWhenActivity_WithChangesOrIndicators_Sends()
    {
        Assert.NotNull(DigestComposer.Compose(Model(changeCount: 5), DigestPolicy.OnlyWhenActivity, "pt"));
        Assert.NotNull(DigestComposer.Compose(Model(indicatorCount: 2), DigestPolicy.OnlyWhenActivity, "pt"));
    }

    [Fact]
    public void Always_NoActivity_StillSends()
    {
        var message = DigestComposer.Compose(Model(), DigestPolicy.Always, "pt");
        Assert.NotNull(message);
    }

    [Fact]
    public void Build_ProducesAllRenderings()
    {
        var message = DigestComposer.Build(Model(newFindings: 3, changeCount: 5, indicatorCount: 2), "en");
        Assert.False(string.IsNullOrWhiteSpace(message.Subject));
        Assert.Contains("<", message.HtmlBody);
        Assert.Contains("Direnix", message.TextBody);
        Assert.Contains("\"identityScore\":71", message.JsonPayload);
        Assert.Contains("\"newFindings24h\":3", message.JsonPayload);
    }

    [Fact]
    public void HasActivity_ReflectsAnySignal()
    {
        Assert.False(DigestComposer.HasActivity(Model()));
        Assert.True(DigestComposer.HasActivity(Model(newFindings: 1)));
        Assert.True(DigestComposer.HasActivity(Model(changeCount: 1)));
        Assert.True(DigestComposer.HasActivity(Model(indicatorCount: 1)));
    }
}
