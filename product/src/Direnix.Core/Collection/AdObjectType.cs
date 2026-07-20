namespace Direnix.Core.Collection;

/// <summary>
/// Tipos de objeto de diretorio suportados pela coleta de Nivel A (read-only LDAP).
/// Alinha com `docs/COLLECTION_ACCESS_AND_SCOPE.md` secao 5.2.
/// </summary>
public enum AdObjectType
{
    User,
    Computer,
    Group,
    OrganizationalUnit,
    GroupPolicyContainer,
    Domain
}
