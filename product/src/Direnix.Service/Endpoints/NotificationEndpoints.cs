using Direnix.Core.Notifications;
using Direnix.Service.Notifications;

namespace Direnix.Service.Endpoints;

/// <summary>Requisição de config vinda da UI. A senha é opcional (null = manter).</summary>
public sealed record NotificationConfigRequest(
    bool SmtpEnabled,
    string SmtpHost,
    int SmtpPort,
    bool SmtpUseStartTls,
    string SmtpUsername,
    string? SmtpPassword,
    string SmtpFrom,
    string SmtpTo,
    bool WebhookEnabled,
    string WebhookUrl,
    string Policy,
    string Lang);

/// <summary>Config exposta à UI — nunca inclui a senha, só se ela existe.</summary>
public sealed record NotificationConfigView(
    bool SmtpEnabled,
    string SmtpHost,
    int SmtpPort,
    bool SmtpUseStartTls,
    string SmtpUsername,
    bool SmtpHasPassword,
    string SmtpFrom,
    string SmtpTo,
    bool WebhookEnabled,
    string WebhookUrl,
    string Policy,
    string Lang,
    DigestOutcome? LastOutcome);

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/notifications");

        group.MapGet("/config", async (NotificationService service, CancellationToken cancellationToken) =>
        {
            var config = await service.GetConfigAsync(cancellationToken);
            var last = await service.GetLastOutcomeAsync(cancellationToken);
            return Results.Ok(new NotificationConfigView(
                config.Smtp.Enabled,
                config.Smtp.Host,
                config.Smtp.Port,
                config.Smtp.UseStartTls,
                config.Smtp.Username,
                !string.IsNullOrEmpty(config.Smtp.ProtectedPassword),
                config.Smtp.FromAddress,
                config.Smtp.ToAddresses,
                config.Webhook.Enabled,
                config.Webhook.Url,
                config.Policy.ToString(),
                config.Lang,
                last));
        });

        group.MapPut("/config", async (NotificationConfigRequest request, NotificationService service, CancellationToken cancellationToken) =>
        {
            var policy = Enum.TryParse<DigestPolicy>(request.Policy, ignoreCase: true, out var p) ? p : DigestPolicy.OnlyWhenActivity;
            var lang = string.Equals(request.Lang, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "pt";
            var port = request.SmtpPort is > 0 and <= 65535 ? request.SmtpPort : 587;

            var config = new NotificationConfig(
                new SmtpSettings(
                    request.SmtpEnabled,
                    request.SmtpHost?.Trim() ?? string.Empty,
                    port,
                    request.SmtpUseStartTls,
                    request.SmtpUsername?.Trim() ?? string.Empty,
                    ProtectedPassword: string.Empty, // resolvido em SaveConfigAsync
                    request.SmtpFrom?.Trim() ?? string.Empty,
                    request.SmtpTo?.Trim() ?? string.Empty),
                new WebhookSettings(request.WebhookEnabled, request.WebhookUrl?.Trim() ?? string.Empty),
                policy,
                lang);

            await service.SaveConfigAsync(config, request.SmtpPassword, cancellationToken);
            return Results.Ok(new { saved = true });
        });

        group.MapPost("/test", async (NotificationService service, CancellationToken cancellationToken) =>
        {
            var outcome = await service.SendTestAsync(cancellationToken);
            return Results.Ok(outcome);
        });

        return endpoints;
    }
}
