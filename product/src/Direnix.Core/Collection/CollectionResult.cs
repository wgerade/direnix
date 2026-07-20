namespace Direnix.Core.Collection;

/// <summary>
/// Cobertura do run conforme `docs/DATA_LIFECYCLE_MODEL.md`.
/// </summary>
public enum CoverageMode
{
    NoDirectory,
    Partial,
    StandardOrFull
}

/// <summary>
/// Estado da coleta de um tipo de objeto: usado para degradar por capacidade
/// (`CapabilityMissing`) sem mascarar como "compliant".
/// </summary>
public sealed record ObjectTypeOutcome(
    AdObjectType ObjectType,
    CapabilityState State,
    int Count,
    string? Message);

/// <summary>
/// Resultado completo de um run de coleta, antes da persistencia.
/// </summary>
public sealed record CollectionResult(
    string RunId,
    string CollectionId,
    string CollectionType,
    CoverageMode CoverageMode,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    string? DomainDn,
    string? DomainName,
    DirectoryNamingContexts NamingContexts,
    IReadOnlyList<CollectedObject> Objects,
    IReadOnlyList<ObjectTypeOutcome> Outcomes,
    IReadOnlyList<string> FeaturePacks,
    CollectionDepth Depth,
    string CollectorVersion,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors)
{
    /// <summary>
    /// GUIDs encontrados na Lixeira do AD (CN=Deleted Objects) neste run. Usado
    /// para classificar a resolucao de um achado cujo objeto desapareceu: se o GUID
    /// esta aqui, foi "Removido — na Lixeira do AD"; senao, "Removido — confirmado".
    /// </summary>
    public IReadOnlyList<string> DeletedObjectGuids { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Em reavaliacoes dirigidas, as chaves dos objetos efetivamente em escopo. A
    /// reconciliacao so resolve achados destes objetos (nao do tipo inteiro).
    /// <c>null</c> = run normal (escopo = tipos coletados).
    /// </summary>
    public IReadOnlyList<string>? ScopeObjectKeys { get; init; }

    /// <summary>Objetos que casaram com cada indicador customizado neste run.</summary>
    public IReadOnlyList<CustomIndicatorMatch> CustomIndicatorMatches { get; init; } = Array.Empty<CustomIndicatorMatch>();

    public IReadOnlyDictionary<AdObjectType, int> InventoryCounts =>
        Outcomes.ToDictionary(outcome => outcome.ObjectType, outcome => outcome.Count);
}
