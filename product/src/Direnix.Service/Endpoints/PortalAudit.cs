using Direnix.Core.Audit;
using Direnix.Core.Storage;

namespace Direnix.Service.Endpoints;

/// <summary>
/// Registro de auditoria das acoes executadas no portal. Sem login ainda, o ator e
/// a conta do servico + o IP de origem; o campo Operador (quando informado) entra
/// nos detalhes. A auditoria nunca interrompe a acao (falha e engolida).
/// </summary>
internal static class PortalAudit
{
    public static async Task LogAsync(
        IProductStore store,
        HttpContext http,
        string action,
        string targetType,
        string targetId,
        string result,
        IReadOnlyDictionary<string, string>? details = null)
    {
        try
        {
            var ev = new AuditEvent(
                DateTimeOffset.UtcNow,
                $"{Environment.UserDomainName}\\{Environment.UserName}",
                "LocalAdmin",
                action,
                targetType,
                targetId,
                http.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                Environment.MachineName,
                result,
                details ?? new Dictionary<string, string>());
            await store.AppendAuditAsync(ev, CancellationToken.None);
        }
        catch
        {
            // Auditoria e best-effort: nunca quebra a acao do usuario.
        }
    }
}
