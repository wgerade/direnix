namespace Direnix.Core.Collection;

/// <summary>
/// Objeto observado em um run de coleta. Mantem os atributos LDAP como dicionario
/// multi-valor para preservar fidelidade; regras usam <see cref="AdAttributes"/>
/// para extrair valores tipados.
/// </summary>
public sealed record CollectedObject(
    AdObjectType ObjectType,
    string DistinguishedName,
    string? ObjectGuid,
    string? ObjectSid,
    string? SamAccountName,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Attributes)
{
    /// <summary>
    /// Chave estavel do objeto, preferindo GUID (imutavel) e caindo para o DN.
    /// Usada para timeline e para a `stableFindingKey`.
    /// </summary>
    public string ObjectKey =>
        !string.IsNullOrWhiteSpace(ObjectGuid)
            ? $"guid:{ObjectGuid}"
            : $"dn:{DistinguishedName}";

    public string DisplayName =>
        !string.IsNullOrWhiteSpace(SamAccountName)
            ? SamAccountName!
            : DistinguishedName;

    public IReadOnlyList<string> Values(string attribute) =>
        Attributes.TryGetValue(attribute, out var values) ? values : Array.Empty<string>();

    public string? Value(string attribute)
    {
        var values = Values(attribute);
        return values.Count > 0 ? values[0] : null;
    }
}
