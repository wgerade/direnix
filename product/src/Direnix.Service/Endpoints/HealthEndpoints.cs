using Direnix.Core.Storage;

namespace Direnix.Service.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health/live", () => Results.Ok(new
        {
            status = "Live",
            service = "Direnix.Service",
            timestamp = DateTimeOffset.UtcNow
        }));

        endpoints.MapGet("/health/ready", async (
            IProductStore store,
            CancellationToken cancellationToken) =>
        {
            var storage = await store.CheckHealthAsync(cancellationToken);
            var status = storage.KeyAvailable && storage.SchemaAvailable ? "Ready" : "NotReady";

            return Results.Ok(new
            {
                status,
                storage
            });
        });

        return endpoints;
    }
}
