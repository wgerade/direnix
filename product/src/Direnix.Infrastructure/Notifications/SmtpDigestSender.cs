using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using Direnix.Core.Notifications;

namespace Direnix.Infrastructure.Notifications;

/// <summary>Envia o digest por SMTP. A senha vem protegida (DPAPI) na config.</summary>
public sealed class SmtpDigestSender : IDigestSender
{
    private readonly ISecretProtector protector;

    public SmtpDigestSender(ISecretProtector protector)
    {
        this.protector = protector;
    }

    public string Channel => "smtp";

    public async Task<NotificationResult> SendAsync(NotificationConfig config, DigestMessage message, CancellationToken cancellationToken)
    {
        var smtp = config.Smtp;
        if (string.IsNullOrWhiteSpace(smtp.Host) || string.IsNullOrWhiteSpace(smtp.FromAddress) || string.IsNullOrWhiteSpace(smtp.ToAddresses))
        {
            return new NotificationResult(false, Channel, "SMTP incompleto: host, remetente e destinatário são obrigatórios.");
        }

        try
        {
            using var mail = new MailMessage
            {
                From = new MailAddress(smtp.FromAddress),
                Subject = message.Subject,
                SubjectEncoding = Encoding.UTF8,
                Body = message.TextBody,
                BodyEncoding = Encoding.UTF8,
                IsBodyHtml = false
            };

            foreach (var recipient in SplitRecipients(smtp.ToAddresses))
            {
                mail.To.Add(recipient);
            }

            if (mail.To.Count == 0)
            {
                return new NotificationResult(false, Channel, "Nenhum destinatário válido.");
            }

            var htmlView = AlternateView.CreateAlternateViewFromString(message.HtmlBody, Encoding.UTF8, MediaTypeNames.Text.Html);
            mail.AlternateViews.Add(htmlView);

            using var client = new SmtpClient(smtp.Host, smtp.Port)
            {
                EnableSsl = smtp.UseStartTls,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            var password = protector.Unprotect(smtp.ProtectedPassword);
            client.Credentials = !string.IsNullOrWhiteSpace(smtp.Username)
                ? new NetworkCredential(smtp.Username, password ?? string.Empty)
                : CredentialCache.DefaultNetworkCredentials;

            await client.SendMailAsync(mail, cancellationToken);
            return new NotificationResult(true, Channel, $"Enviado para {mail.To.Count} destinatário(s).");
        }
        catch (Exception ex) when (ex is SmtpException or InvalidOperationException or FormatException or IOException)
        {
            return new NotificationResult(false, Channel, ex.Message);
        }
    }

    private static IEnumerable<string> SplitRecipients(string value) =>
        value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
