using System.Reflection;
using Direnix.Core.Reporting;
using Direnix.Core.Storage;
using Direnix.Service.Configuration;
using Microsoft.Extensions.Options;

namespace Direnix.Service.Reporting;

/// <summary>
/// Monta o <see cref="ReportModel"/> a partir do store. Fonte única usada tanto
/// pelo endpoint de relatório quanto pelo digest de notificações.
/// </summary>
public sealed class ReportModelBuilder
{
    private readonly IProductStore store;
    private readonly ProductHostOptions hostOptions;

    public ReportModelBuilder(IProductStore store, IOptions<ProductHostOptions> hostOptions)
    {
        this.store = store;
        this.hostOptions = hostOptions.Value;
    }

    public static string ProductVersion()
    {
        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)
            ?? "dev";
        var plusIndex = version.IndexOf('+');
        return plusIndex > 0 ? version[..plusIndex] : version;
    }

    public async Task<ReportModel> BuildAsync(CancellationToken cancellationToken)
    {
        var dashboard = await store.GetDashboardAsync(cancellationToken);
        var topFindings = await store.GetFindingsAsync("active", null, 15, 0, cancellationToken);
        var indicators = await store.GetLatestIndicatorsAsync(cancellationToken);
        var changeSummary = await store.GetChangeSummaryAsync(24, cancellationToken);
        var newFindings = await store.CountNewFindingsAsync(24, cancellationToken);

        return new ReportModel(
            DateTimeOffset.UtcNow,
            ProductVersion(),
            $"http://{hostOptions.ListenAddress}:{hostOptions.Port}/",
            dashboard.LatestRun?.DomainName,
            dashboard.LatestRun?.CoverageMode ?? "NoCollection",
            dashboard.LatestRun?.CompletedAt,
            dashboard.IdentityScore,
            dashboard.Tier0Score,
            dashboard.ActiveFindings,
            dashboard.SeverityBreakdown,
            dashboard.CategoryBreakdown,
            topFindings,
            indicators,
            changeSummary,
            newFindings,
            dashboard.Inventory);
    }
}
