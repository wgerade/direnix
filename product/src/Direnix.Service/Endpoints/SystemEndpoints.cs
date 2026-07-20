using Direnix.Core.Identity;
using Direnix.Infrastructure.Storage;
using Direnix.Service.Configuration;
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

        return endpoints;
    }
}
