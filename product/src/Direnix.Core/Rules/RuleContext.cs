using Direnix.Core.Collection;

namespace Direnix.Core.Rules;

/// <summary>
/// Contexto derivado de um run, compartilhado por todas as regras.
/// Pre-computa indices caros (privilegio efetivo direto, DCs, dominio).
/// </summary>
public sealed class RuleContext
{
    private static readonly int[] PrivilegedRids = [512, 518, 519, 520, 544, 548, 549, 550, 551];

    public RuleContext(
        IReadOnlyList<CollectedObject> objects,
        string domainName,
        RuleThresholds thresholds,
        BusinessCriticality criticality,
        DateTimeOffset asOf)
    {
        Objects = objects;
        DomainName = domainName;
        Thresholds = thresholds;
        Criticality = criticality;
        AsOf = asOf;

        var dnIndex = new Dictionary<string, CollectedObject>(StringComparer.OrdinalIgnoreCase);
        foreach (var obj in objects)
        {
            dnIndex[obj.DistinguishedName] = obj;
        }

        DnIndex = dnIndex;

        DomainRoot = objects.FirstOrDefault(o => o.ObjectType == AdObjectType.Domain);

        DomainControllerDns = objects
            .Where(o => o.ObjectType == AdObjectType.Computer &&
                        AdAttributes.HasFlag(o.Value("userAccountControl"), UserAccountControlFlags.ServerTrustAccount))
            .Select(o => o.DistinguishedName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        PrivilegedMembership = BuildPrivilegedMembership(objects);

        // DNs que possuem ao menos um objeto-filho coletado (para OU vazia).
        var withChildren = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var obj in objects)
        {
            var dn = obj.DistinguishedName;
            var comma = dn.IndexOf(',');
            if (comma > 0 && comma < dn.Length - 1)
            {
                withChildren.Add(dn[(comma + 1)..]);
            }
        }

        DnsWithChildren = withChildren;

        // GUIDs de GPO referenciados por gPLink em dominio + OUs + sites.
        var linked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var obj in objects.Where(o => o.ObjectType is AdObjectType.Domain or AdObjectType.OrganizationalUnit))
        {
            foreach (var guid in obj.Values("gPLink").SelectMany(ExtractGpoGuids))
            {
                linked.Add(guid);
            }
        }

        var siteGuids = DomainRoot?.Value("siteLinkedGpoGuids");
        if (!string.IsNullOrWhiteSpace(siteGuids))
        {
            foreach (var guid in siteGuids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                linked.Add(guid.ToLowerInvariant());
            }
        }

        LinkedGpoGuids = linked;
    }

    public IReadOnlyList<CollectedObject> Objects { get; }
    public string DomainName { get; }
    public RuleThresholds Thresholds { get; }
    public BusinessCriticality Criticality { get; }
    public DateTimeOffset AsOf { get; }
    public IReadOnlyDictionary<string, CollectedObject> DnIndex { get; }
    public CollectedObject? DomainRoot { get; }
    public IReadOnlySet<string> DomainControllerDns { get; }

    /// <summary>DNs de contêineres/OUs que têm ao menos um objeto-filho coletado.</summary>
    public IReadOnlySet<string> DnsWithChildren { get; }

    /// <summary>GUIDs (minúsculo, sem chaves) de GPOs vinculadas em domínio/OUs/sites.</summary>
    public IReadOnlySet<string> LinkedGpoGuids { get; }

    /// <summary>Mapa DN do membro -> grupos privilegiados (por nome) dos quais e membro direto.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> PrivilegedMembership { get; }

    public bool IsPrivileged(CollectedObject obj) =>
        PrivilegedMembership.ContainsKey(obj.DistinguishedName) ||
        AdAttributes.ParseInt(obj.Value("adminCount")) == 1;

    /// <summary>
    /// Principal bem-conhecido / interno (RID &lt; 1000): Administrator (500),
    /// Guest (501), krbtgt (502), DefaultAccount (503) e grupos internos.
    /// Sao criados pelo proprio AD, nao podem ser removidos e seu estado e por
    /// design; reporta-los em regras de higiene gera ruido acionavel zero.
    /// Regras especificas (ex.: rotacao de krbtgt) tratam esses objetos
    /// explicitamente e nao usam este filtro.
    /// </summary>
    public static bool IsWellKnownBuiltIn(CollectedObject obj) =>
        AdAttributes.RelativeId(obj.ObjectSid) is (not null) and < 1000;

    /// <summary>
    /// Conta gerenciada/maquina cujo segredo o proprio AD rotaciona (gMSA, sMSA,
    /// conta de computador): sAMAccountName termina em '$'. Regras de senha nao se aplicam.
    /// </summary>
    public static bool IsManagedAccount(CollectedObject obj) =>
        obj.SamAccountName?.EndsWith('$') == true;

    public IEnumerable<CollectedObject> OfType(AdObjectType type) =>
        Objects.Where(o => o.ObjectType == type);

    /// <summary>Extrai GUIDs de GPO (minúsculo, sem chaves) de um valor gPLink.</summary>
    private static IEnumerable<string> ExtractGpoGuids(string gpLink)
    {
        if (string.IsNullOrWhiteSpace(gpLink))
        {
            yield break;
        }

        foreach (var token in gpLink.Split('[', ']', StringSplitOptions.RemoveEmptyEntries))
        {
            var start = token.IndexOf("cn={", StringComparison.OrdinalIgnoreCase);
            if (start < 0)
            {
                continue;
            }

            start += 3;
            var end = token.IndexOf('}', start);
            if (end > start)
            {
                yield return token.Substring(start, end - start + 1).Trim('{', '}').ToLowerInvariant();
            }
        }
    }

    private static Dictionary<string, IReadOnlyList<string>> BuildPrivilegedMembership(
        IReadOnlyList<CollectedObject> objects)
    {
        var membership = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in objects.Where(o => o.ObjectType == AdObjectType.Group))
        {
            var rid = AdAttributes.RelativeId(group.ObjectSid);
            if (rid is null || !PrivilegedRids.Contains(rid.Value))
            {
                continue;
            }

            var groupName = group.SamAccountName ?? group.DistinguishedName;
            foreach (var memberDn in group.Values("member"))
            {
                if (!membership.TryGetValue(memberDn, out var groups))
                {
                    groups = [];
                    membership[memberDn] = groups;
                }

                if (!groups.Contains(groupName))
                {
                    groups.Add(groupName);
                }
            }
        }

        return membership.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<string>)pair.Value,
            StringComparer.OrdinalIgnoreCase);
    }
}
