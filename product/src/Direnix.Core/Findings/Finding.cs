namespace Direnix.Core.Findings;

/// <summary>
/// Finding normalizado produzido por uma regra. A timeline usa
/// <see cref="StableFindingKey"/> (regra + objeto + condicao) conforme
/// `docs/DIRENIX_RULES_AND_INDICATORS.md`.
/// </summary>
public sealed record Finding(
    string StableFindingKey,
    string RuleId,
    string Title,
    FindingCategory Category,
    Severity Severity,
    FindingDecision Decision,
    FindingStatus Status,
    int BusinessRiskScore,
    string DomainName,
    string ObjectKey,
    string ObjectDisplay,
    IReadOnlyDictionary<string, string> Evidence)
{
    public static string BuildStableKey(string ruleId, string objectKey) =>
        $"{ruleId}::{objectKey}";
}
