using Direnix.Core.Findings;

namespace Direnix.Core.Scoring;

/// <summary>Entrada de pontuação: severidade do achado ativo e se é Tier 0 (acesso privilegiado).</summary>
public readonly record struct ScoreInput(Severity Severity, bool IsTier0);

/// <summary>Resultado consolidado da postura de identidade.</summary>
public readonly record struct ScoreResult(int Score, int Tier0Score, string Health);

/// <summary>
/// Identity Score = 100 menos penalidades ponderadas dos achados ativos (clamp 0–100).
/// Tier0 Score considera só achados de acesso privilegiado. Saúde: verde ≥90, amarelo ≥70, senão vermelho.
/// </summary>
public static class IdentityScore
{
    private static int Weight(Severity severity) => severity switch
    {
        Severity.Critical => 25,
        Severity.High => 10,
        Severity.Medium => 4,
        Severity.Low => 1,
        _ => 0
    };

    public static string HealthFor(int score) => score switch
    {
        >= 90 => "green",
        >= 70 => "yellow",
        _ => "red"
    };

    public static ScoreResult Compute(IEnumerable<ScoreInput> inputs)
    {
        var list = inputs as ICollection<ScoreInput> ?? inputs.ToList();

        var totalPenalty = list.Sum(i => Weight(i.Severity));
        var tier0Penalty = list.Where(i => i.IsTier0).Sum(i => Weight(i.Severity));

        var score = Math.Clamp(100 - totalPenalty, 0, 100);
        var tier0 = Math.Clamp(100 - tier0Penalty, 0, 100);
        return new ScoreResult(score, tier0, HealthFor(score));
    }
}
