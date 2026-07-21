using System.Text;
using Direnix.Core.Reporting;
using Direnix.Core.Storage;
using Direnix.Service.Reporting;

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
            ReportModelBuilder modelBuilder,
            string? lang,
            CancellationToken cancellationToken) =>
        {
            var model = await modelBuilder.BuildAsync(cancellationToken);
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
}
