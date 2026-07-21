using System.Text.Json;
using System.Text.Json.Serialization;
using Direnix.Core.Audit;
using Direnix.Core.Notifications;
using Direnix.Core.Storage;
using Direnix.Infrastructure.Notifications;
using Direnix.Service.Reporting;

namespace Direnix.Service.Notifications;

/// <summary>Resultado do último disparo de digest (para a UI).</summary>
public sealed record DigestOutcome(
    DateTimeOffset At,
    bool Skipped,
    string? SkipReason,
    IReadOnlyList<NotificationResult> Results);

/// <summary>
/// Persiste a configuração de notificações (KV app_settings), protege a senha SMTP
/// via DPAPI e despacha o digest pelos canais habilitados. Fonte única usada pelo
/// endpoint (config/teste) e pelo agendador (digest pós-coleta).
/// </summary>
public sealed class NotificationService
{
    private const string ConfigKey = "notifications_config";
    private const string LastResultKey = "notifications_last_result";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IProductStore store;
    private readonly ReportModelBuilder modelBuilder;
    private readonly ISecretProtector protector;
    private readonly SmtpDigestSender smtp;
    private readonly WebhookDigestSender webhook;
    private readonly ILogger<NotificationService> logger;

    public NotificationService(
        IProductStore store,
        ReportModelBuilder modelBuilder,
        ISecretProtector protector,
        SmtpDigestSender smtp,
        WebhookDigestSender webhook,
        ILogger<NotificationService> logger)
    {
        this.store = store;
        this.modelBuilder = modelBuilder;
        this.protector = protector;
        this.smtp = smtp;
        this.webhook = webhook;
        this.logger = logger;
    }

    public async Task<NotificationConfig> GetConfigAsync(CancellationToken cancellationToken)
    {
        var json = await store.GetSettingAsync(ConfigKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
        {
            return NotificationConfig.Default();
        }

        try
        {
            return JsonSerializer.Deserialize<NotificationConfig>(json, JsonOptions) ?? NotificationConfig.Default();
        }
        catch (JsonException)
        {
            return NotificationConfig.Default();
        }
    }

    /// <summary>
    /// Salva a config. <paramref name="newPlaintextPassword"/>: null = manter a senha
    /// atual; string vazia = limpar; não-vazia = proteger e gravar.
    /// </summary>
    public async Task SaveConfigAsync(NotificationConfig incoming, string? newPlaintextPassword, CancellationToken cancellationToken)
    {
        var current = await GetConfigAsync(cancellationToken);

        var protectedPassword = newPlaintextPassword switch
        {
            null => current.Smtp.ProtectedPassword,          // manter
            "" => string.Empty,                               // limpar
            _ => protector.Protect(newPlaintextPassword)      // definir
        };

        var toSave = incoming with
        {
            Smtp = incoming.Smtp with { ProtectedPassword = protectedPassword }
        };

        await store.SetSettingAsync(ConfigKey, JsonSerializer.Serialize(toSave, JsonOptions), cancellationToken);
    }

    public async Task<DigestOutcome?> GetLastOutcomeAsync(CancellationToken cancellationToken)
    {
        var json = await store.GetSettingAsync(LastResultKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<DigestOutcome>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Envio de teste: monta o digest com os dados atuais e envia sempre.</summary>
    public async Task<DigestOutcome> SendTestAsync(CancellationToken cancellationToken)
    {
        var config = await GetConfigAsync(cancellationToken);
        var model = await modelBuilder.BuildAsync(cancellationToken);
        var message = DigestComposer.Build(model, config.Lang);
        var results = await SendAsync(config, message, cancellationToken);
        var outcome = new DigestOutcome(DateTimeOffset.UtcNow, Skipped: false, SkipReason: null, results);
        await RecordAsync(outcome, "test", cancellationToken);
        return outcome;
    }

    /// <summary>Disparo automático pós-coleta: respeita a política de envio.</summary>
    public async Task<DigestOutcome> SendScheduledDigestAsync(CancellationToken cancellationToken)
    {
        var config = await GetConfigAsync(cancellationToken);
        if (!config.Smtp.Enabled && !config.Webhook.Enabled)
        {
            return new DigestOutcome(DateTimeOffset.UtcNow, Skipped: true, SkipReason: "Nenhum canal habilitado.", Array.Empty<NotificationResult>());
        }

        var model = await modelBuilder.BuildAsync(cancellationToken);
        var message = DigestComposer.Compose(model, config.Policy, config.Lang);
        if (message is null)
        {
            var skipped = new DigestOutcome(DateTimeOffset.UtcNow, Skipped: true, SkipReason: "Sem atividade no período (política OnlyWhenActivity).", Array.Empty<NotificationResult>());
            await RecordAsync(skipped, "schedule", cancellationToken);
            return skipped;
        }

        var results = await SendAsync(config, message, cancellationToken);
        var outcome = new DigestOutcome(DateTimeOffset.UtcNow, Skipped: false, SkipReason: null, results);
        await RecordAsync(outcome, "schedule", cancellationToken);
        return outcome;
    }

    private async Task<IReadOnlyList<NotificationResult>> SendAsync(NotificationConfig config, DigestMessage message, CancellationToken cancellationToken)
    {
        var results = new List<NotificationResult>();
        if (config.Smtp.Enabled)
        {
            results.Add(await smtp.SendAsync(config, message, cancellationToken));
        }
        if (config.Webhook.Enabled)
        {
            results.Add(await webhook.SendAsync(config, message, cancellationToken));
        }
        return results;
    }

    private async Task RecordAsync(DigestOutcome outcome, string trigger, CancellationToken cancellationToken)
    {
        try
        {
            await store.SetSettingAsync(LastResultKey, JsonSerializer.Serialize(outcome, JsonOptions), cancellationToken);

            var summary = outcome.Skipped
                ? outcome.SkipReason ?? "skipped"
                : string.Join("; ", outcome.Results.Select(r => $"{r.Channel}:{(r.Ok ? "ok" : "fail")}"));
            await store.AppendAuditAsync(new AuditEvent(
                DateTimeOffset.UtcNow,
                $"{Environment.UserDomainName}\\{Environment.UserName}",
                "notifications",
                "DigestSent",
                "Notification",
                trigger,
                "127.0.0.1",
                Environment.MachineName,
                outcome.Skipped || outcome.Results.All(r => r.Ok) ? "Success" : "Warning",
                new Dictionary<string, string> { ["trigger"] = trigger, ["result"] = summary }),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao registrar resultado do digest.");
        }
    }
}
