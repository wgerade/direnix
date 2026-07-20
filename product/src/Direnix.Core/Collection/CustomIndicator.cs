namespace Direnix.Core.Collection;

/// <summary>
/// Consulta LDAP read-only de um indicador customizado, ja normalizada (o filtro
/// PowerShell do usuario e traduzido para filtro LDAP antes de chegar aqui). O
/// coletor executa como qualquer outra busca — nunca ha execucao de PowerShell.
/// </summary>
public sealed record CustomIndicatorQuery(
    string Id,
    string Name,
    string LdapFilter,
    AdObjectType ObjectType);

/// <summary>
/// Objetos que casaram com um indicador customizado num run. Reaproveita
/// <see cref="CollectedObject"/> para o drill-down (display, DN, SID).
/// </summary>
public sealed record CustomIndicatorMatch(
    string IndicatorId,
    string Name,
    IReadOnlyList<CollectedObject> Objects);
