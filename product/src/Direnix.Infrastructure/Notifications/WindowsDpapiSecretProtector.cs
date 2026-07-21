using System.Security.Cryptography;
using System.Text;
using Direnix.Core.Notifications;

namespace Direnix.Infrastructure.Notifications;

/// <summary>
/// Protege segredos (senha SMTP) com Windows DPAPI (LocalMachine) — mesmo esquema
/// da chave do banco. O valor guardado é o blob protegido em base64.
/// </summary>
public sealed class WindowsDpapiSecretProtector : ISecretProtector
{
    // Prefixo para reconhecer valores já protegidos e não re-proteger por engano.
    private const string Marker = "dpapi:";

    public string Protect(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
            return string.Empty;
        }

        var bytes = Encoding.UTF8.GetBytes(plaintext);
        var protectedBytes = ProtectedData.Protect(bytes, optionalEntropy: null, DataProtectionScope.LocalMachine);
        Array.Clear(bytes, 0, bytes.Length);
        return Marker + Convert.ToBase64String(protectedBytes);
    }

    public string? Unprotect(string protectedValue)
    {
        if (string.IsNullOrEmpty(protectedValue) || !protectedValue.StartsWith(Marker, StringComparison.Ordinal))
        {
            return null;
        }

        try
        {
            var protectedBytes = Convert.FromBase64String(protectedValue[Marker.Length..]);
            var bytes = ProtectedData.Unprotect(protectedBytes, optionalEntropy: null, DataProtectionScope.LocalMachine);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException)
        {
            return null;
        }
    }
}
