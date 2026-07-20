using Direnix.Core.Collection;
using Direnix.Core.Findings;

namespace Direnix.Core.Rules;

/// <summary>
/// Agrega o catalogo de regras de higiene e as avalia sobre um run coletado.
/// </summary>
public sealed class HygieneRuleEngine
{
    private readonly IReadOnlyList<IAdHygieneRule> rules;

    public HygieneRuleEngine(IEnumerable<IAdHygieneRule>? rules = null)
    {
        // Importante: sob injecao de dependencia, um parametro de colecao sem
        // implementacoes registradas chega como colecao VAZIA (nao null). Por isso
        // o fallback considera tambem o caso vazio, senao o motor roda sem regras.
        var provided = rules?.ToList();
        this.rules = provided is { Count: > 0 } ? provided : DefaultRules();
    }

    public static IReadOnlyList<IAdHygieneRule> DefaultRules() =>
    [
        new StaleUserRule(),
        new StaleComputerRule(),
        new DisabledUserRetentionRule(),
        new MachineAccountQuotaRule(),
        new KerberosPreauthRule(),
        new UnconstrainedDelegationRule(),
        new PrivilegedGroupExposureRule(),
        new KrbtgtAgeRule(),
        new RecycleBinRule(),
        new EmptyGroupRule(),
        new EmptyOuRule(),
        new UnlinkedGpoRule(),
        new EmptyGpoRule(),
        new DisabledGpoRule(),
        new PasswordNotRequiredRule(),
        new PasswordNeverExpiresRule(),
        new ReversiblePasswordRule(),
        new KerberoastRule(),
        new LegacyOsComputerRule(),
        new OrphanAdminCountRule(),
        new ConflictObjectsRule(),
        new LostAndFoundRule()
    ];

    public IReadOnlyList<string> RuleIds => rules.Select(rule => rule.RuleId).ToList();

    public IReadOnlyList<Finding> Evaluate(
        CollectionResult result,
        RuleThresholds? thresholds = null,
        BusinessCriticality? criticality = null)
    {
        var context = new RuleContext(
            result.Objects,
            result.DomainName ?? DeriveDomainName(result.DomainDn) ?? "(desconhecido)",
            thresholds ?? new RuleThresholds(),
            criticality ?? new BusinessCriticality(),
            result.CompletedAt);

        var findings = new List<Finding>();
        foreach (var rule in rules)
        {
            findings.AddRange(rule.Evaluate(context));
        }

        return findings;
    }

    /// <summary>Avalia aplicando um perfil: pula regras desabilitadas e usa os thresholds do perfil.</summary>
    public IReadOnlyList<Finding> Evaluate(CollectionResult result, RuleProfile profile)
    {
        var context = new RuleContext(
            result.Objects,
            result.DomainName ?? DeriveDomainName(result.DomainDn) ?? "(desconhecido)",
            profile.Thresholds,
            new BusinessCriticality(),
            result.CompletedAt);

        var findings = new List<Finding>();
        foreach (var rule in rules.Where(r => profile.IsRuleEnabled(r.RuleId)))
        {
            findings.AddRange(rule.Evaluate(context));
        }

        return findings;
    }

    /// <summary>Converte um DN de dominio (`DC=corp,DC=local`) em nome (`corp.local`).</summary>
    public static string? DeriveDomainName(string? domainDn)
    {
        if (string.IsNullOrWhiteSpace(domainDn))
        {
            return null;
        }

        var parts = domainDn
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part => part.StartsWith("DC=", StringComparison.OrdinalIgnoreCase))
            .Select(part => part[3..]);

        var name = string.Join('.', parts);
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }
}
