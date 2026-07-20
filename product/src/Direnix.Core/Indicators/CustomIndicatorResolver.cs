using Direnix.Core.Collection;
using Direnix.Core.Rules;

namespace Direnix.Core.Indicators;

/// <summary>
/// Converte os indicadores customizados HABILITADOS de um perfil em consultas LDAP
/// read-only que o coletor executa junto da coleta (manual e agendada). Entradas
/// invalidas (filtro nao traduzivel) sao ignoradas aqui — a validacao com mensagem
/// acontece no momento de salvar o perfil.
/// </summary>
public static class CustomIndicatorResolver
{
    public static IReadOnlyList<CustomIndicatorQuery> Resolve(RuleProfile profile)
    {
        var queries = new List<CustomIndicatorQuery>();
        foreach (var def in profile.CustomIndicators.Where(c => c.Enabled))
        {
            if (!LdapFilterExtractor.TryBuild(def.Kind, def.Query, out var filter, out _))
            {
                continue;
            }

            var type = Enum.TryParse<AdObjectType>(def.ObjectType, ignoreCase: true, out var parsed)
                ? parsed
                : AdObjectType.User;
            queries.Add(new CustomIndicatorQuery(def.Id, def.Name, filter, type));
        }

        return queries;
    }
}
