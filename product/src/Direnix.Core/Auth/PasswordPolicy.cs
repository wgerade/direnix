namespace Direnix.Core.Auth;

/// <summary>
/// Política mínima de senha do portal e parâmetros de proteção contra força bruta.
/// Mantida no Core para ser testável e compartilhada por setup/criação/reset/troca.
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 10;

    // Bloqueio por força bruta: após N falhas seguidas, tranca por T minutos.
    public const int MaxFailedAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Valida a força da senha. Retorna null se OK, ou uma mensagem de erro.
    /// Regra: mínimo <see cref="MinLength"/> caracteres e pelo menos 3 das 4 classes
    /// (maiúscula, minúscula, dígito, símbolo).
    /// </summary>
    public static string? Validate(string? password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinLength)
        {
            return $"A senha deve ter ao menos {MinLength} caracteres.";
        }

        var classes = 0;
        if (password.Any(char.IsUpper)) classes++;
        if (password.Any(char.IsLower)) classes++;
        if (password.Any(char.IsDigit)) classes++;
        if (password.Any(c => !char.IsLetterOrDigit(c))) classes++;

        return classes >= 3
            ? null
            : "A senha deve combinar ao menos 3 destes: maiúsculas, minúsculas, números e símbolos.";
    }
}
