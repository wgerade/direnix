namespace Direnix.Service.Configuration;

/// <summary>
/// Sinaliza que o processo roda em modo portátil: sem Windows Service, data root
/// por-usuário em %LOCALAPPDATA%, e portal de sessão única (loopback) sem tela de
/// login — pensado para "avaliação única" estilo executável que abre e roda.
/// </summary>
public sealed class PortableModeState
{
    public bool IsPortable { get; init; }

    public string Operator { get; init; } = "portable";

    /// <summary>
    /// Ativo quando o processo foi iniciado com --portable ou o executável se chama
    /// DirenixPortable (mesmo binário, comportamento pela identidade do exe).
    /// </summary>
    public static bool Detect(string[] args)
    {
        if (args.Any(a => string.Equals(a, "--portable", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        var exeName = Path.GetFileNameWithoutExtension(Environment.ProcessPath ?? string.Empty);
        return string.Equals(exeName, "DirenixPortable", StringComparison.OrdinalIgnoreCase);
    }

    public static string DefaultDataRoot() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Direnix", "Portable", "data");
}
