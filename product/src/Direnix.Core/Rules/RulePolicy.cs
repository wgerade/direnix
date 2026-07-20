using Direnix.Core.Findings;

namespace Direnix.Core.Rules;

/// <summary>
/// Thresholds configuraveis por perfil. Defaults alinham com
/// `config/business-rules.example.json` (perfil MicrosoftDefault).
/// </summary>
public sealed record RuleThresholds
{
    public int StaleUserDays { get; init; } = 90;
    public int StaleComputerDays { get; init; } = 90;
    public int DisabledObjectRetentionDays { get; init; } = 180;
    public int DormantSensitiveEntityDays { get; init; } = 180;
    public int KrbtgtRotationDays { get; init; } = 180;
    public int MachineAccountQuotaExpected { get; init; }

    /// <summary>Minimo recomendado de retencao de objetos deletados (dias) na Lixeira do AD.</summary>
    public int RecycleBinMinRetentionDays { get; init; } = 180;

    /// <summary>Dias minimos que um grupo/OU precisa estar vazio para ser reportado (evita pegar algo recem-criado/esvaziado).</summary>
    public int EmptyObjectMinDays { get; init; } = 30;
}

/// <summary>
/// Multiplicadores de negocio (secao 7.2 do doc de regras).
/// </summary>
public sealed record BusinessCriticality
{
    public int Tier0Multiplier { get; init; } = 30;
    public int MissingOwnerMultiplier { get; init; } = 10;
    public int RecurringFindingMultiplier { get; init; } = 10;
    public int LowEvidenceReduction { get; init; } = 15;
}

/// <summary>
/// Calcula `businessRiskScore` a partir da severidade base e dos modificadores.
/// businessRiskScore = min(100, technicalRiskBase + businessMultipliers - compensating).
/// </summary>
public static class RiskScoring
{
    /// <summary>Base tecnica = limite inferior da faixa de severidade (secao 7.3).</summary>
    public static int TechnicalBase(Severity severity) => severity switch
    {
        Severity.Critical => 90,
        Severity.High => 70,
        Severity.Medium => 40,
        Severity.Low => 10,
        _ => 0
    };

    public static int Compute(Severity severity, int businessMultipliers = 0, int compensating = 0) =>
        Math.Clamp(TechnicalBase(severity) + businessMultipliers - compensating, 0, 100);

    public static Severity SeverityFromScore(int score) => score switch
    {
        >= 90 => Severity.Critical,
        >= 70 => Severity.High,
        >= 40 => Severity.Medium,
        >= 10 => Severity.Low,
        _ => Severity.Info
    };
}
