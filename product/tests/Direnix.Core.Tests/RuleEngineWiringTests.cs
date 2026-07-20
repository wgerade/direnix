using Direnix.Core.Rules;
using Xunit;

namespace Direnix.Core.Tests;

public class RuleEngineWiringTests
{
    [Fact]
    public void DefaultConstructor_LoadsRules() =>
        Assert.NotEmpty(new HygieneRuleEngine().RuleIds);

    // Reproduz o cenario de injecao de dependencia: colecao vazia injetada.
    // Sem o fallback, o motor rodaria sem regras e nunca geraria achados.
    [Fact]
    public void EmptyRuleSet_FallsBackToDefaults() =>
        Assert.NotEmpty(new HygieneRuleEngine(Array.Empty<IAdHygieneRule>()).RuleIds);

    [Fact]
    public void NullRuleSet_FallsBackToDefaults() =>
        Assert.NotEmpty(new HygieneRuleEngine(null).RuleIds);
}
