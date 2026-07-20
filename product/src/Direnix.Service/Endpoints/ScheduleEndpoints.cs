using Direnix.Core.Collection;
using Direnix.Core.Scheduling;
using Direnix.Core.Storage;
using Direnix.Infrastructure.Directory;
using Direnix.Service.Configuration;

namespace Direnix.Service.Endpoints;

/// <summary>
/// Configuração da coleta automática. Não há campo de credencial: a coleta roda sob
/// a identidade do serviço (gMSA) via Kerberos/Negotiate.
/// </summary>
public static class ScheduleEndpoints
{
    public static IEndpointRouteBuilder MapScheduleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/schedule", async (IProductStore store, CancellationToken ct) =>
            Results.Ok(await store.GetScheduleConfigAsync(ct)));

        endpoints.MapPut("/api/v1/schedule", async (ScheduleConfig input, IProductStore store, HttpContext http, CancellationToken ct) =>
        {
            // Protocolo sempre LDAPS para o canal endurecido.
            var normalized = input with
            {
                Protocol = "Ldaps",
                Host = input.Host?.Trim() ?? string.Empty
            };
            normalized = normalized with { NextRunAt = ScheduleCalculator.Next(normalized, DateTimeOffset.Now) };

            if (normalized.Enabled && string.IsNullOrWhiteSpace(normalized.Host))
            {
                return Results.BadRequest(new { error = "Informe o controlador de dominio (host) para agendar." });
            }

            await store.SaveScheduleConfigAsync(normalized, ct);
            await PortalAudit.LogAsync(store, http, "ScheduleSaved", "Schedule", normalized.Enabled ? "enabled" : "disabled", "Success",
                new Dictionary<string, string> { ["frequency"] = normalized.Frequency.ToString(), ["host"] = normalized.Host });
            return Results.Ok(await store.GetScheduleConfigAsync(ct));
        });

        // Testa conectividade como a IDENTIDADE DO SERVICO (sem credencial): valida a gMSA.
        endpoints.MapPost("/api/v1/schedule/test", async (ScheduleTestRequest req, IAdDirectoryProbe probe, IProductStore store, HttpContext http, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Host))
            {
                return Results.BadRequest(new { error = "Informe o host." });
            }
            var target = new DirectoryTarget(req.Host.Trim(), DirectoryProtocol.Ldaps, 636, null, TimeSpan.FromSeconds(15));
            var result = await probe.ProbeRootDseAsync(target, ct);
            var ok = result.State is CapabilityState.Ready or CapabilityState.ReadyWithWarnings;
            await PortalAudit.LogAsync(store, http, "ScheduleTested", "Directory", req.Host.Trim(), ok ? "Success" : "Failure");
            return Results.Ok(new
            {
                ok,
                state = result.State.ToString(),
                identity = $"{Environment.UserDomainName}\\{Environment.UserName}",
                namingContext = result.NamingContexts.DefaultNamingContext,
                errors = result.Errors
            });
        });

        // Estado/conta de logon/tipo de inicializacao do PROPRIO servico Windows.
        endpoints.MapGet("/api/v1/service/status", () =>
            Results.Ok(WindowsServiceControl.GetStatus()));

        // Aplica tipo de inicializacao + identidade (LocalSystem ou gMSA sem senha) via sc.exe.
        endpoints.MapPost("/api/v1/service/config", async (ServiceConfigRequest req, IProductStore store, HttpContext http, CancellationToken ct) =>
        {
            var result = WindowsServiceControl.Apply(req.StartupType, req.IdentityMode, req.AccountName);
            await PortalAudit.LogAsync(store, http, "ServiceConfigChanged", "WindowsService", req.IdentityMode, result.Ok ? "Success" : "Failure",
                new Dictionary<string, string> { ["startup"] = req.StartupType, ["account"] = req.AccountName ?? "" });
            return result.Ok
                ? Results.Ok(new { ok = true, message = result.Message, status = WindowsServiceControl.GetStatus() })
                : Results.BadRequest(new { ok = false, error = result.Message });
        });

        return endpoints;
    }
}

public sealed record ScheduleTestRequest(string Host);
public sealed record ServiceConfigRequest(string StartupType, string IdentityMode, string? AccountName);
