namespace Direnix.Core.Collection;

/// <summary>
/// Pedido de coleta read-only. O escopo de diretorio (search base) e descoberto
/// via RootDSE quando nao informado explicitamente.
/// Ver `docs/COLLECTION_ACCESS_AND_SCOPE.md`.
/// </summary>
public sealed record CollectionRequest(
    DirectoryTarget Target,
    IReadOnlyList<AdObjectType> ObjectTypes,
    IReadOnlyList<string> FeaturePacks,
    CollectionDepth Depth,
    string PolicyProfile = "MicrosoftDefault",
    string? SearchBaseOverride = null,
    int MaxObjectsPerType = 50_000,
    string? RequestedByUserId = null,
    string? Operator = null)
{
    /// <summary>
    /// Reavaliacao dirigida: quando preenchido, o coletor ignora o escopo amplo e
    /// le apenas estes objetos (busca Base por DN). Usado pelo "Reavaliar
    /// selecionados". Vazio = coleta normal por tipo.
    /// </summary>
    public IReadOnlyList<FocusObject> FocusObjects { get; init; } = Array.Empty<FocusObject>();

    /// <summary>
    /// Indicadores customizados (do perfil) a executar junto da coleta: buscas LDAP
    /// read-only adicionais. Vazio = nenhum indicador customizado.
    /// </summary>
    public IReadOnlyList<CustomIndicatorQuery> CustomIndicators { get; init; } = Array.Empty<CustomIndicatorQuery>();

    public bool IsFocused => FocusObjects.Count > 0;

    public static CollectionRequest Inventory(DirectoryTarget target) =>
        new(
            target,
            [AdObjectType.User, AdObjectType.Computer, AdObjectType.Group, AdObjectType.OrganizationalUnit, AdObjectType.GroupPolicyContainer],
            ["Inventory", "CleanupHygiene", "PrivilegedAccess"],
            CollectionDepth.Standard);
}

/// <summary>
/// Objeto-alvo de uma reavaliacao dirigida. <see cref="ObjectKey"/> e a chave
/// estavel original (preserva o escopo da reconciliacao mesmo se o objeto foi
/// removido); DN + tipo dizem ao coletor o que buscar (busca Base no DN).
/// </summary>
public sealed record FocusObject(string ObjectKey, string DistinguishedName, AdObjectType Type);
