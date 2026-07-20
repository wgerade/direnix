using Direnix.Core.Findings;

namespace Direnix.Core.Rules;

/// <summary>
/// Regra de avaliacao read-only. Cada regra pode gerar zero ou mais findings
/// (`MVP_IMPLEMENTATION_PLAN.md`, secao 15.4 do doc de regras).
/// </summary>
public interface IAdHygieneRule
{
    string RuleId { get; }

    IEnumerable<Finding> Evaluate(RuleContext context);
}
