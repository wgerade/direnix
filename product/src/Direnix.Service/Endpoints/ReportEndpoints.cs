using System.Reflection;
using System.Text;
using Direnix.Core.Reporting;
using Direnix.Core.Storage;
using Direnix.Service.Configuration;
using Microsoft.Extensions.Options;

namespace Direnix.Service.Endpoints;

public static class ReportEndpoints
{
    // BOM UTF-8 explicito para o Excel reconhecer acentuacao nos CSVs.
    private static readonly UTF8Encoding Utf8WithBom = new(encoderShouldEmitUTF8Identifier: true);

    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/reports");

        group.MapGet("/summary", async (
            IProductStore store,
            HttpContext http,
            IOptions<ProductHostOptions> hostOptions,
            string? lang,
            CancellationToken cancellationToken) =>
        {
            var model = await BuildModelAsync(store, hostOptions.Value, cancellationToken);
            var html = ReportBuilder.BuildHtml(model, NormalizeLang(lang));
            await PortalAudit.LogAsync(store, http, "ReportExported", "Report", "summary", "Success");
            return Results.File(
                Encoding.UTF8.GetBytes(html),
                "text/html; charset=utf-8",
                $"direnix-report-{model.GeneratedAt.ToLocalTime():yyyyMMdd-HHmm}.html");
        });

        group.MapGet("/findings.csv", async (
            IProductStore store,
            HttpContext http,
            string? view,
            string? category,
            string? lang,
            CancellationToken cancellationToken) =>
        {
            var rows = await store.GetFindingsAsync(view ?? "active", category, 0, 0, cancellationToken);
            var csv = ReportBuilder.BuildFindingsCsv(rows, NormalizeLang(lang));
            await PortalAudit.LogAsync(store, http, "ReportExported", "Report", "findings.csv", "Success",
                new Dictionary<string, string> { ["view"] = view ?? "active", ["rows"] = rows.Count.ToString() });
            return Results.File(
                Utf8WithBom.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray(),
                "text/csv; charset=utf-8",
                $"direnix-riscos-{DateTimeOffset.Now:yyyyMMdd-HHmm}.csv");
        });

        group.MapGet("/inventory.csv", async (
            IProductStore store,
            HttpContext http,
            string? lang,
            CancellationToken cancellationToken) =>
        {
            var inventory = await store.GetCurrentInventoryAsync(cancellationToken);
            var csv = ReportBuilder.BuildInventoryCsv(inventory, NormalizeLang(lang));
            await PortalAudit.LogAsync(store, http, "ReportExported", "Report", "inventory.csv", "Success");
            return Results.File(
                Utf8WithBom.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray(),
                "text/csv; charset=utf-8",
                $"direnix-inventario-{DateTimeOffset.Now:yyyyMMdd-HHmm}.csv");
        });

        return endpoints;
    }

    private static string NormalizeLang(string? lang) =>
        string.Equals(lang, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "pt";

    private static async Task<ReportModel> BuildModelAsync(
        IProductStore store,
        ProductHostOptions hostOptions,
        CancellationToken cancellationToken)
    {
        var dashboard = await store.GetDashboardAsync(cancellationToken);
        var topFindings = await store.GetFindingsAsync("active", null, 15, 0, cancellationToken);
        var indicators = await store.GetLatestIndicatorsAsync(cancellationToken);
        var changeSummary = await store.GetChangeSummaryAsync(24, cancellationToken);
        var newFindings = await store.CountNewFindingsAsync(24, cancellationToken);

        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)
            ?? "dev";
        // InformationalVersion pode vir com sufixo de build (+hash); mostra so a parte semantica.
        var plusIndex = version.IndexOf('+');
        if (plusIndex > 0)
        {
            version = version[..plusIndex];
        }

        return new ReportModel(
            DateTimeOffset.UtcNow,
            version,
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
