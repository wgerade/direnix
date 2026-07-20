namespace Direnix.Core.Collection;

/// <summary>
/// Progresso de coleta para alimentar a fila/job na UI.
/// </summary>
public sealed record CollectionProgress(
    string Stage,
    AdObjectType? ObjectType,
    int CollectedSoFar,
    string Message);

/// <summary>
/// Motor de coleta read-only de Active Directory. A implementacao principal usa
/// .NET LDAP/LDAPS (`System.DirectoryServices.Protocols`).
/// </summary>
public interface ICollectionEngine
{
    Task<CollectionResult> CollectAsync(
        CollectionRequest request,
        IProgress<CollectionProgress>? progress,
        CancellationToken cancellationToken);
}
