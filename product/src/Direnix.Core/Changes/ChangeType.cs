namespace Direnix.Core.Changes;

/// <summary>
/// Tipo de mudança observada entre duas coletas de um objeto. Alimenta a Timeline
/// e a Morning View ("o que mudou desde ontem").
/// </summary>
public enum ChangeType
{
    ObjectCreated,
    ObjectDeleted,
    MemberAdded,
    MemberRemoved,
    PrivilegedMemberAdded,
    PrivilegedMemberRemoved,
    AccountEnabled,
    AccountDisabled,
    DangerousFlagSet,
    DangerousFlagCleared,
    AdminCountChanged,
    SpnAdded,
    GpoLinkChanged,
    AttributeChanged
}
