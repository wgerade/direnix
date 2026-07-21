namespace Direnix.Core.Notifications;

/// <summary>
/// Política de envio do digest matinal.
/// </summary>
public enum DigestPolicy
{
    /// <summary>Envia sempre que a coleta agendada termina.</summary>
    Always,

    /// <summary>Só envia quando há atividade (mudanças, novos riscos ou indicadores &gt; 0).</summary>
    OnlyWhenActivity
}

/// <summary>Configuração do canal SMTP. A senha é guardada protegida (DPAPI), nunca em claro.</summary>
public sealed record SmtpSettings(
    bool Enabled,
    string Host,
    int Port,
    bool UseStartTls,
    string Username,
    string ProtectedPassword,
    string FromAddress,
    string ToAddresses)
{
    public static SmtpSettings Default() => new(false, "", 587, true, "", "", "", "");
}

/// <summary>Configuração do canal webhook (POST JSON — cobre Teams/Slack/automations).</summary>
public sealed record WebhookSettings(bool Enabled, string Url)
{
    public static WebhookSettings Default() => new(false, "");
}

/// <summary>Configuração consolidada de notificações (persistida em app_settings).</summary>
public sealed record NotificationConfig(
    SmtpSettings Smtp,
    WebhookSettings Webhook,
    DigestPolicy Policy,
    string Lang)
{
    public static NotificationConfig Default() =>
        new(SmtpSettings.Default(), WebhookSettings.Default(), DigestPolicy.OnlyWhenActivity, "pt");
}

/// <summary>Mensagem de digest já renderizada, pronta para cada canal.</summary>
public sealed record DigestMessage(
    string Subject,
    string HtmlBody,
    string TextBody,
    string JsonPayload);

/// <summary>Resultado de um envio por um canal.</summary>
public sealed record NotificationResult(bool Ok, string Channel, string Detail);

/// <summary>
/// Protege/desprotege segredos (senha SMTP) fora do Core, para manter o Core
/// neutro de plataforma. Implementado na infraestrutura com Windows DPAPI.
/// </summary>
public interface ISecretProtector
{
    string Protect(string plaintext);

    string? Unprotect(string protectedValue);
}

/// <summary>Envia um digest por um canal concreto (SMTP ou webhook).</summary>
public interface IDigestSender
{
    string Channel { get; }

    Task<NotificationResult> SendAsync(NotificationConfig config, DigestMessage message, CancellationToken cancellationToken);
}
