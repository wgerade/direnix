using Direnix.Core.Identity;
using Direnix.Infrastructure.Storage;
using Direnix.Service.Configuration;
using Direnix.Service.Update;
using Microsoft.Extensions.Options;

namespace Direnix.Service.Endpoints;

public static class SystemEndpoints
{
    public static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/system");

        group.MapGet("/about", (
            IOptions<ProductHostOptions> options,
            ProductStorageOptions storageOptions,
            IWebHostEnvironment environment) => Results.Ok(new
        {
            product = "Direnix",
            service = "Direnix.Service",
            platform = ".NET ASP.NET Core Windows Service",
            mode = options.Value.InstanceMode,
            bind = new
            {
                options.Value.ListenAddress,
                options.Value.Port
            },
            portal = new
            {
                url = $"http://{options.Value.ListenAddress}:{options.Value.Port}/",
                root = environment.WebRootPath,
                shortcut = "Start Menu > Direnix > Direnix Portal"
            },
            installation = new
            {
                runtimeRoot = AppContext.BaseDirectory,
                contentRoot = environment.ContentRootPath,
                dataRoot = storageOptions.DataRoot,
                database = storageOptions.DatabasePath
            },
            roles = Enum.GetNames<AppRole>(),
            prototypeStatus = "Legacy PowerShell portal is migration-only and is not the product runtime."
        }));

        // Check de atualização. GET (auto/cache — só bate na rede se ligado e cache
        // expirado) e POST (ligar/desligar e/ou check manual = consentimento explícito).
        group.MapGet("/update", async (UpdateCheckService updates, CancellationToken ct) =>
            Results.Ok(await updates.GetStatusAsync(force: false, ct)));

        group.MapPost("/update", async (UpdatePreference body, UpdateCheckService updates, CancellationToken ct) =>
        {
            if (body.Enabled is not null)
            {
                await updates.SetEnabledAsync(body.Enabled.Value, ct);
            }
            // "check": bate na rede na hora, mesmo desligado (o usuário pediu).
            return Results.Ok(await updates.GetStatusAsync(force: body.Check ?? false, ct));
        });

        return endpoints;
    }
}

/// <summary>Preferência do check de atualização (ligar/desligar e/ou checar agora).</summary>
public sealed record UpdatePreference(bool? Enabled, bool? Check);
