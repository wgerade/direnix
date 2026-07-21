using System.Net.Http;
using System.Text;
using Direnix.Core.Notifications;

namespace Direnix.Infrastructure.Notifications;

/// <summary>
/// Envia o digest como POST JSON para um webhook genérico (cobre Teams/Slack via
/// connectors, automações, SIEM). Payload = <see cref="DigestMessage.JsonPayload"/>.
/// </summary>
public sealed class WebhookDigestSender : IDigestSender
{
    private readonly HttpClient http;

    public WebhookDigestSender(HttpClient http)
    {
        this.http = http;
    }

    public string Channel => "webhook";

    public async Task<NotificationResult> SendAsync(NotificationConfig config, DigestMessage message, CancellationToken cancellationToken)
    {
        var url = config.Webhook.Url;
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return new NotificationResult(false, Channel, "URL de webhook inválida (use http/https).");
        }

        try
        {
            using var content = new StringContent(message.JsonPayload, Encoding.UTF8, "application/json");
            using var response = await http.PostAsync(uri, content, cancellationToken);
            return response.IsSuccessStatusCode
                ? new NotificationResult(true, Channel, $"HTTP {(int)response.StatusCode}.")
                : new NotificationResult(false, Channel, $"HTTP {(int)response.StatusCode}.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new NotificationResult(false, Channel, ex.Message);
        }
    }
}
