using System.Text.Json;
using Direnix.Core.Rules;
using Direnix.Core.Storage;

namespace Direnix.Service.Endpoints;

public static class FindingsEndpoints
{
    public static IEndpointRouteBuilder MapFindingsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/findings", async (
            IProductStore store,
            int? limit,
            int? offset,
            string? category,
            string? view,
            CancellationToken cancellationToken) =>
        {
            // Sem ?limit => 0 => sem teto: o painel de riscos traz todos os achados.
            var rows = await store.GetFindingsAsync(view ?? "active", category, limit ?? 0, offset ?? 0, cancellationToken);
            return Results.Ok(new
            {
                count = rows.Count,
                items = rows.Select(row => new
                {
                    row.StableFindingKey,
                    row.RuleId,
                    row.Title,
                    category = row.Category.ToString(),
                    severity = row.Severity.ToString(),
                    risk = row.BusinessRiskScore,
                    action = row.Decision.ToString(),
                    status = row.Status.ToString(),
                    row.ObjectDisplay,
                    row.ObjectKey,
                    row.FirstSeen,
                    row.LastSeen,
                    row.LastRunId,
                    row.ResolutionReason
                })
            });
        });

        endpoints.MapGet("/api/v1/findings/detail", async (
            IProductStore store,
            string key,
            CancellationToken cancellationToken) =>
        {
            var row = await store.GetFindingAsync(key, cancellationToken);
            if (row is null)
            {
                return Results.NotFound();
            }

            var definition = RuleCatalog.Get(row.RuleId);
            var evidence = ParseEvidence(row.EvidenceJson);
            var preview = FillTemplate(definition.RemediationPreview, evidence, row.ObjectDisplay);
            var apply = FillTemplate(definition.RemediationApply, evidence, row.ObjectDisplay);

            return Results.Ok(new
            {
                row.StableFindingKey,
                row.RuleId,
                row.Title,
                category = row.Category.ToString(),
                severity = row.Severity.ToString(),
                risk = row.BusinessRiskScore,
                action = row.Decision.ToString(),
                status = row.Status.ToString(),
                row.ObjectDisplay,
                row.FirstSeen,
                row.LastSeen,
                row.LastRunId,
                row.ObjectKey,
                row.ResolutionReason,
                definition.BusinessImpact,
                frameworks = new
                {
                    mitre = definition.Mitre,
                    cis = definition.Cis,
                    nist = definition.Nist,
                    microsoft = definition.MicrosoftRef
                },
                remediation = new
                {
                    manual = definition.RemediationManual,
                    preview,
                    apply
                },
                evidence
            });
        });

        endpoints.MapGet("/api/v1/inventory", async (
            IProductStore store,
            CancellationToken cancellationToken) =>
        {
            var latest = await store.GetLatestRunAsync(cancellationToken);
            var inventory = await store.GetCurrentInventoryAsync(cancellationToken);
            return Results.Ok(new
            {
                runId = latest?.RunId,
                domainName = latest?.DomainName,
                items = inventory.Select(item => new
                {
                    item.ObjectType,
                    item.TotalCount,
                    item.LastObservedAt,
                    item.IsCurrent
                })
            });
        });

        endpoints.MapGet("/api/v1/runs", async (
            IProductStore store,
            int? limit,
            CancellationToken cancellationToken) =>
        {
            var runs = await store.GetRunsAsync(limit ?? 50, cancellationToken);
            return Results.Ok(new { count = runs.Count, items = runs });
        });

        endpoints.MapGet("/api/v1/objects/history", async (
            IProductStore store,
            string key,
            CancellationToken cancellationToken) =>
        {
            var entries = await store.GetObjectHistoryAsync(key, cancellationToken);
            return Results.Ok(new { count = entries.Count, items = entries });
        });

        endpoints.MapPost("/api/v1/findings/exception", async (
            ExceptionRequest request,
            IProductStore store,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Key) ||
                string.IsNullOrWhiteSpace(request.Owner) ||
                string.IsNullOrWhiteSpace(request.Justification))
            {
                return Results.BadRequest(new { error = "Informe finding, responsavel e justificativa." });
            }

            var record = await store.AddExceptionAsync(
                new RiskExceptionInput(request.Key, request.Owner.Trim(), request.Justification.Trim(), request.ValidForDays ?? 365),
                cancellationToken);
            await PortalAudit.LogAsync(store, http, "ExceptionAdded", "Finding", request.Key, "Success",
                new Dictionary<string, string> { ["owner"] = request.Owner.Trim() });
            return Results.Ok(record);
        });

        endpoints.MapGet("/api/v1/exceptions", async (
            IProductStore store,
            bool? includeExpired,
            CancellationToken cancellationToken) =>
        {
            var records = await store.GetExceptionViewsAsync(includeExpired ?? false, cancellationToken);
            return Results.Ok(new { count = records.Count, items = records });
        });

        endpoints.MapDelete("/api/v1/exceptions/{id}", async (
            string id,
            IProductStore store,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            await store.RemoveExceptionAsync(id, cancellationToken);
            await PortalAudit.LogAsync(store, http, "ExceptionRemoved", "Exception", id, "Success");
            return Results.NoContent();
        });

        endpoints.MapGet("/api/v1/changes", async (
            IProductStore store,
            int? limit,
            int? sinceHours,
            string? type,
            CancellationToken cancellationToken) =>
        {
            var rows = await store.GetChangesAsync(limit ?? 200, sinceHours, type, cancellationToken);
            return Results.Ok(new { count = rows.Count, items = rows });
        });

        endpoints.MapGet("/api/v1/changes/summary", async (
            IProductStore store,
            int? sinceHours,
            CancellationToken cancellationToken) =>
        {
            var hours = sinceHours ?? 24;
            var rows = await store.GetChangeSummaryAsync(hours, cancellationToken);
            var newFindings = await store.CountNewFindingsAsync(hours, cancellationToken);
            return Results.Ok(new { items = rows, newFindings });
        });

        endpoints.MapGet("/api/v1/search", async (
            IProductStore store,
            string? q,
            CancellationToken cancellationToken) =>
        {
            var hits = await store.SearchObjectsAsync(q ?? string.Empty, 50, cancellationToken);
            return Results.Ok(new { count = hits.Count, items = hits });
        });

        endpoints.MapGet("/api/v1/objects/detail", async (
            IProductStore store,
            string key,
            CancellationToken cancellationToken) =>
        {
            var detail = await store.GetObjectDetailAsync(key, cancellationToken);
            return detail is null ? Results.NotFound() : Results.Ok(detail);
        });

        endpoints.MapGet("/api/v1/audit", async (
            IProductStore store,
            int? limit,
            CancellationToken cancellationToken) =>
        {
            var events = await store.GetAuditEventsAsync(limit ?? 200, cancellationToken);
            return Results.Ok(new { count = events.Count, items = events });
        });

        // Indicadores operacionais do run mais recente (cards do painel + drill-down).
        endpoints.MapGet("/api/v1/indicators", async (
            IProductStore store,
            CancellationToken cancellationToken) =>
        {
            var rows = await store.GetLatestIndicatorsAsync(cancellationToken);
            return Results.Ok(new { count = rows.Count, items = rows });
        });

        // Catalogo dos indicadores built-in (para os toggles de configuracao).
        endpoints.MapGet("/api/v1/indicators/catalog", () =>
            Results.Ok(new
            {
                items = Direnix.Core.Indicators.IndicatorCatalog.All.Select(d => new
                {
                    d.Id,
                    d.Title,
                    d.Category,
                    d.Description
                })
            }));

        // Valida/traduz uma consulta de indicador customizado (LDAP ou PowerShell)
        // SEM executar nada: so devolve o filtro LDAP resultante ou o erro.
        endpoints.MapPost("/api/v1/indicators/validate", (ValidateIndicatorRequest request) =>
        {
            var ok = Direnix.Core.Indicators.LdapFilterExtractor.TryBuild(
                request.Kind, request.Query, out var filter, out var error);
            return Results.Ok(new { ok, ldapFilter = ok ? filter : null, error });
        });

        endpoints.MapGet("/api/v1/rules/catalog", () =>
            Results.Ok(new
            {
                items = RuleCatalog.All.Select(d => new
                {
                    d.RuleId,
                    d.Title,
                    category = d.Category.ToString(),
                    action = d.Action.ToString()
                })
            }));

        endpoints.MapGet("/api/v1/settings/profiles", async (
            IProductStore store,
            CancellationToken cancellationToken) =>
        {
            var state = await store.GetRuleProfilesAsync(cancellationToken);
            return Results.Ok(state);
        });

        endpoints.MapPut("/api/v1/settings/profiles", async (
            RuleProfilesState state,
            IProductStore store,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            // Diff antes/depois para auditar a acao REAL (criar/editar/excluir/ativar),
            // ja que o endpoint recebe o estado inteiro de uma vez.
            var before = await store.GetRuleProfilesAsync(cancellationToken);
            await store.SaveRuleProfilesAsync(state, cancellationToken);
            await AuditProfileChangesAsync(store, http, before, state);
            return Results.Ok(await store.GetRuleProfilesAsync(cancellationToken));
        });

        return endpoints;
    }

    private static async Task AuditProfileChangesAsync(
        IProductStore store,
        HttpContext http,
        RuleProfilesState before,
        RuleProfilesState after)
    {
        var beforeMap = before.Profiles.ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);
        var afterMap = after.Profiles.ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

        foreach (var p in after.Profiles.Where(p => !p.BuiltIn))
        {
            if (!beforeMap.ContainsKey(p.Name))
            {
                await PortalAudit.LogAsync(store, http, "ProfileCreated", "RuleProfile", p.Name, "Success");
            }
            else if (JsonSerializer.Serialize(beforeMap[p.Name]) != JsonSerializer.Serialize(p))
            {
                await PortalAudit.LogAsync(store, http, "ProfileUpdated", "RuleProfile", p.Name, "Success");
            }
        }

        foreach (var p in before.Profiles.Where(p => !p.BuiltIn && !afterMap.ContainsKey(p.Name)))
        {
            await PortalAudit.LogAsync(store, http, "ProfileDeleted", "RuleProfile", p.Name, "Success");
        }

        if (!string.Equals(before.ActiveProfile, after.ActiveProfile, StringComparison.OrdinalIgnoreCase))
        {
            await PortalAudit.LogAsync(store, http, "ProfileActivated", "RuleProfile", after.ActiveProfile, "Success");
        }
    }

    private static IReadOnlyDictionary<string, string> ParseEvidence(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>();
        }
    }

    private static string FillTemplate(string template, IReadOnlyDictionary<string, string> evidence, string objectDisplay)
    {
        if (string.IsNullOrEmpty(template))
        {
            return template;
        }

        var sam = evidence.GetValueOrDefault("sam") ?? objectDisplay;
        var dn = evidence.GetValueOrDefault("distinguishedName") ?? objectDisplay;
        var domain = evidence.GetValueOrDefault("domain") ?? objectDisplay;
        var guid = evidence.GetValueOrDefault("guid") ?? string.Empty;
        var name = evidence.GetValueOrDefault("name") ?? objectDisplay;

        return template
            .Replace("{sam}", sam)
            .Replace("{dn}", dn)
            .Replace("{domain}", domain)
            .Replace("{guid}", guid)
            .Replace("{name}", name)
            .Replace("{group}", evidence.GetValueOrDefault("privilegedGroups") ?? string.Empty);
    }
}

public sealed record ExceptionRequest(string Key, string Owner, string Justification, int? ValidForDays);

public sealed record ValidateIndicatorRequest(string? Kind, string? Query);
