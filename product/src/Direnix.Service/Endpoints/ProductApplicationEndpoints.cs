using Direnix.Core.Storage;
using Direnix.Infrastructure.Storage;
using Direnix.Service.Configuration;
using Microsoft.Extensions.Options;

namespace Direnix.Service.Endpoints;

public static class ProductApplicationEndpoints
{
    private static readonly (string Key, string Label, string Unit)[] MetricCatalog =
    [
        ("riskScore", "riskScore", "0-100"),
        ("findings", "activeRisks", "items"),
        ("staleObjects", "staleObjects", "objects"),
        ("privilegedExposure", "privilegedExposure", "items")
    ];

    public static IEndpointRouteBuilder MapProductApplicationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/app");

        group.MapGet("/shell", async (
            IProductStore store,
            ProductStorageOptions storageOptions,
            IOptions<ProductHostOptions> hostOptions,
            CancellationToken cancellationToken) =>
        {
            var storage = await store.CheckHealthAsync(cancellationToken);
            var storageReady = storage.KeyAvailable && storage.SchemaAvailable;
            var dashboard = await store.GetDashboardAsync(cancellationToken);
            var latest = dashboard.LatestRun;
            var hasRun = latest is not null;

            var metricLookup = dashboard.Metrics.ToDictionary(m => m.Key, m => m.Value);
            var metrics = MetricCatalog.Select(entry =>
            {
                var hasValue = metricLookup.TryGetValue(entry.Key, out var value);
                return new
                {
                    key = entry.Key,
                    label = entry.Label,
                    value = hasValue ? value : (int?)null,
                    unit = entry.Unit,
                    state = hasValue ? "measured" : "unmeasured"
                };
            });

            return Results.Ok(new
            {
                generatedAt = DateTimeOffset.UtcNow,
                product = new
                {
                    name = "Direnix",
                    mode = hostOptions.Value.InstanceMode,
                    portalUrl = $"http://{hostOptions.Value.ListenAddress}:{hostOptions.Value.Port}/"
                },
                dataContext = new
                {
                    source = "real-store",
                    hasCompletedCollection = hasRun,
                    latestRunId = latest?.RunId,
                    latestRunStartedAt = latest?.StartedAt,
                    latestRunCompletedAt = latest?.CompletedAt,
                    domainName = latest?.DomainName,
                    coverageMode = latest?.CoverageMode ?? "NoCollection"
                },
                metrics,
                inventory = dashboard.Inventory.Select(item => new
                {
                    item.ObjectType,
                    item.TotalCount,
                    item.IsCurrent
                }),
                severityBreakdown = dashboard.SeverityBreakdown.Select(item => new
                {
                    severity = item.Severity.ToString(),
                    item.Count
                }),
                categoryBreakdown = dashboard.CategoryBreakdown.Select(item => new
                {
                    category = item.Category.ToString(),
                    item.Count
                }),
                activeFindings = dashboard.ActiveFindings,
                identityScore = dashboard.IdentityScore,
                tier0Score = dashboard.Tier0Score,
                health = dashboard.Health,
                criticalFindings = dashboard.SeverityBreakdown
                    .Where(s => s.Severity == Direnix.Core.Findings.Severity.Critical)
                    .Sum(s => s.Count),
                workspaces = new[]
                {
                    new { key = "overview", label = "Painel", state = "available" },
                    new { key = "collect", label = "Coletas", state = "available" },
                    new { key = "findings", label = "Achados", state = hasRun ? "available" : "waiting-for-data" },
                    new { key = "inventory", label = "Inventario", state = hasRun ? "available" : "waiting-for-data" },
                    new { key = "remediation", label = "Remediacao", state = "waiting-for-data" },
                    new { key = "reports", label = "Relatorios", state = "waiting-for-data" },
                    new { key = "settings", label = "Configuracoes", state = "requires-bootstrap" },
                    new { key = "operations", label = "Operacao", state = "available" }
                },
                emptyState = hasRun ? null : new
                {
                    title = "Nenhuma coleta concluida",
                    reason = "O banco esta pronto. Aponte um controlador de dominio e inicie a primeira coleta LDAPS.",
                    nextGate = "Validar conectividade e iniciar coleta"
                },
                capabilities = new
                {
                    storageReady,
                    encryptedDatabase = storageReady,
                    localAuthentication = false,
                    rbac = false,
                    ldapsCollection = true,
                    remediationWorkbench = false,
                    reportExport = false
                },
                installation = new
                {
                    serviceName = "Direnix.Service",
                    bind = new
                    {
                        hostOptions.Value.ListenAddress,
                        hostOptions.Value.Port
                    },
                    dataRoot = storageOptions.DataRoot,
                    database = storageOptions.DatabasePath
                }
            });
        });

        return endpoints;
    }
}
