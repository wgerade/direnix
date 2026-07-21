using Xunit;

namespace Direnix.Core.Tests;

/// <summary>
/// Comparação de versões do check de atualização. A lógica espelha
/// <c>UpdateCheckService.IsNewer</c> (que vive no assembly do Service, sem referência
/// aqui) — este teste fixa o contrato esperado.
/// </summary>
public class UpdateVersionTests
{
    private static bool IsNewer(string? latest, string current)
    {
        if (string.IsNullOrWhiteSpace(latest))
        {
            return false;
        }

        static string Normalize(string v)
        {
            var cut = v.IndexOfAny(new[] { '-', '+' });
            return cut > 0 ? v[..cut] : v;
        }

        return Version.TryParse(Normalize(latest), out var l)
            && Version.TryParse(Normalize(current), out var c)
            && l > c;
    }

    [Theory]
    [InlineData("0.9.0", "0.8.0", true)]
    [InlineData("1.0.0", "0.9.9", true)]
    [InlineData("0.8.1", "0.8.0", true)]
    [InlineData("0.8.0", "0.8.0", false)]      // igual não é update
    [InlineData("0.7.6", "0.8.0", false)]      // remoto mais antigo
    [InlineData("0.9.0-dev", "0.8.0", true)]   // sufixo de pré-lançamento ignorado
    [InlineData(null, "0.8.0", false)]         // sem versão remota → silencioso
    [InlineData("", "0.8.0", false)]
    [InlineData("garbage", "0.8.0", false)]    // não-parseável → silencioso
    public void ComparesSemanticVersions(string? latest, string current, bool expected)
    {
        Assert.Equal(expected, IsNewer(latest, current));
    }
}
