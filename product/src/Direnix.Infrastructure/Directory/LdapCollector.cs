using System.DirectoryServices.Protocols;
using System.Net;
using System.Security.Principal;
using Direnix.Core.Collection;
using DirectoryProtocol = Direnix.Core.Collection.DirectoryProtocol;

namespace Direnix.Infrastructure.Directory;

/// <summary>
/// Motor de coleta read-only baseado em LDAP/LDAPS paginado.
/// Coleta de Nivel A (`docs/COLLECTION_ACCESS_AND_SCOPE.md`): users, computers,
/// groups, OUs, GPOs e o objeto raiz do dominio. Degrada por capacidade quando
/// um tipo nao pode ser lido (`CapabilityMissing`), sem mascarar como compliant.
/// </summary>
public sealed class LdapCollector : ICollectionEngine
{
    private const int PageSize = 1000;
    private const string CollectorVersion = "ldap-collector/0.1";

    private static readonly string[] RootDseAttributes =
    [
        "defaultNamingContext",
        "configurationNamingContext",
        "schemaNamingContext",
        "rootDomainNamingContext"
    ];

    private sealed record TypePlan(
        AdObjectType Type,
        Func<string, string> SearchBase,
        string Filter,
        SearchScope Scope,
        string[] Attributes);

    public Task<CollectionResult> CollectAsync(
        CollectionRequest request,
        IProgress<CollectionProgress>? progress,
        CancellationToken cancellationToken) =>
        Task.Run(() => Collect(request, progress, cancellationToken), cancellationToken);

    private CollectionResult Collect(
        CollectionRequest request,
        IProgress<CollectionProgress>? progress,
        CancellationToken cancellationToken)
    {
        var runId = Guid.NewGuid().ToString("N");
        var startedAt = DateTimeOffset.UtcNow;
        var warnings = new List<string>();
        var errors = new List<string>();
        var outcomes = new List<ObjectTypeOutcome>();
        var objects = new List<CollectedObject>();

        if (request.Target.Protocol == DirectoryProtocol.Ldap)
        {
            warnings.Add("LDAP sem TLS permitido apenas como excecao explicita de laboratorio.");
        }

        DirectoryNamingContexts namingContexts;
        using var connection = CreateConnection(request.Target);

        try
        {
            namingContexts = ReadRootDse(connection, request.Target.Timeout);
        }
        catch (Exception ex) when (ex is LdapException or DirectoryOperationException or InvalidOperationException)
        {
            errors.Add($"Falha ao consultar RootDSE: {ex.Message}");
            return Failed(runId, startedAt, warnings, errors);
        }

        var searchBase = request.SearchBaseOverride ?? namingContexts.DefaultNamingContext;
        if (string.IsNullOrWhiteSpace(searchBase))
        {
            errors.Add("RootDSE nao retornou defaultNamingContext; sem base de busca.");
            return Failed(runId, startedAt, warnings, errors);
        }

        progress?.Report(new CollectionProgress("RootDSE", null, 0, $"Base de busca: {searchBase}"));

        // Objeto raiz do dominio (para regras de dominio como MachineAccountQuota e
        // Lixeira do AD). Numa reavaliacao dirigida so e lido se o foco incluir Domain.
        var collectDomain = !request.IsFocused || request.FocusObjects.Any(f => f.Type == AdObjectType.Domain);
        if (collectDomain)
        {
            try
            {
                var domainEntries = RunSearch(
                    connection,
                    searchBase,
                    "(objectClass=*)",
                    SearchScope.Base,
                    AttributesFor(AdObjectType.Domain),
                    1,
                    request.Target.Timeout,
                    cancellationToken);
                if (domainEntries.Count > 0)
                {
                    var domain = MapEntry(domainEntries[0], AdObjectType.Domain);
                    domain = EnrichDomainWithConfig(connection, namingContexts.ConfigurationNamingContext, request.Target.Timeout, domain, warnings);
                    domain = EnrichDomainWithHealth(connection, searchBase, request.Target.Timeout, domain, warnings);
                    objects.Add(domain);
                }
            }
            catch (Exception ex) when (ex is LdapException or DirectoryOperationException)
            {
                warnings.Add($"Nao foi possivel ler atributos do dominio: {ex.Message}");
            }
        }

        // Lixeira do AD: GUIDs de objetos deletados, para classificar remocoes na
        // reconciliacao (na Lixeira vs confirmado). Falha vira aviso, nao quebra o run.
        var deletedGuids = new List<string>();
        try
        {
            deletedGuids = CollectDeletedObjectGuids(connection, searchBase, request.Target.Timeout, cancellationToken);
        }
        catch (Exception ex) when (ex is LdapException or DirectoryOperationException)
        {
            warnings.Add($"Nao foi possivel ler a Lixeira do AD: {ex.Message}");
        }

        if (request.IsFocused)
        {
            CollectFocused(connection, request, objects, outcomes, warnings, progress, cancellationToken);
        }
        else
        {
            foreach (var plan in BuildPlans(request.ObjectTypes, searchBase))
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(new CollectionProgress("Collect", plan.Type, objects.Count, $"Avaliando {plan.Type}"));

                try
                {
                    var entries = RunSearch(
                        connection,
                        plan.SearchBase(searchBase),
                        plan.Filter,
                        plan.Scope,
                        plan.Attributes,
                        request.MaxObjectsPerType,
                        request.Target.Timeout,
                        cancellationToken);

                    foreach (var entry in entries)
                    {
                        objects.Add(MapEntry(entry, plan.Type));
                    }

                    outcomes.Add(new ObjectTypeOutcome(plan.Type, CapabilityState.Ready, entries.Count, null));
                    progress?.Report(new CollectionProgress("Collected", plan.Type, objects.Count, $"{plan.Type}: {entries.Count}"));
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex) when (ex is LdapException or DirectoryOperationException)
                {
                    outcomes.Add(new ObjectTypeOutcome(plan.Type, CapabilityState.CapabilityMissing, 0, ex.Message));
                    warnings.Add($"{plan.Type}: capacidade ausente ({ex.Message}).");
                }
            }
        }

        // Indicadores customizados (do perfil): buscas LDAP read-only adicionais.
        // So na coleta normal (nao em reavaliacao dirigida). Falha isolada vira aviso.
        var customMatches = new List<CustomIndicatorMatch>();
        if (!request.IsFocused && request.CustomIndicators.Count > 0)
        {
            foreach (var indicator in request.CustomIndicators)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var matches = CollectCustomIndicator(connection, indicator, searchBase, request, cancellationToken);
                    customMatches.Add(matches);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex) when (ex is LdapException or DirectoryOperationException)
                {
                    warnings.Add($"Indicador '{indicator.Name}': {ex.Message}");
                }
            }
        }

        var completedAt = DateTimeOffset.UtcNow;
        var coverage = request.IsFocused
            ? CoverageMode.Partial
            : (outcomes.Any(o => o.State == CapabilityState.Ready)
                ? (outcomes.Any(o => o.State == CapabilityState.CapabilityMissing) ? CoverageMode.Partial : CoverageMode.StandardOrFull)
                : CoverageMode.NoDirectory);

        return new CollectionResult(
            runId,
            runId,
            request.IsFocused ? "Focused" : "Scoped",
            coverage,
            startedAt,
            completedAt,
            searchBase,
            Core.Rules.HygieneRuleEngine.DeriveDomainName(searchBase),
            namingContexts,
            objects,
            outcomes,
            request.FeaturePacks,
            request.Depth,
            CollectorVersion,
            warnings,
            errors)
        {
            DeletedObjectGuids = deletedGuids,
            CustomIndicatorMatches = customMatches
        };
    }

    /// <summary>
    /// Executa a busca LDAP read-only de um indicador customizado. A base varia com
    /// o tipo (GPOs vivem em CN=Policies); o filtro e o do usuario, ja normalizado.
    /// </summary>
    private static CustomIndicatorMatch CollectCustomIndicator(
        LdapConnection connection,
        CustomIndicatorQuery indicator,
        string domainDn,
        CollectionRequest request,
        CancellationToken cancellationToken)
    {
        var searchBase = indicator.ObjectType == AdObjectType.GroupPolicyContainer
            ? $"CN=Policies,CN=System,{domainDn}"
            : domainDn;

        var entries = RunSearch(
            connection,
            searchBase,
            indicator.LdapFilter,
            SearchScope.Subtree,
            AttributesFor(indicator.ObjectType),
            Math.Min(request.MaxObjectsPerType, 20_000),
            request.Target.Timeout,
            cancellationToken);

        var objects = entries.Select(e => MapEntry(e, indicator.ObjectType)).ToList();
        return new CustomIndicatorMatch(indicator.Id, indicator.Name, objects);
    }

    private static CollectionResult Failed(
        string runId,
        DateTimeOffset startedAt,
        IReadOnlyList<string> warnings,
        IReadOnlyList<string> errors) =>
        new(
            runId,
            runId,
            "Scoped",
            CoverageMode.NoDirectory,
            startedAt,
            DateTimeOffset.UtcNow,
            null,
            null,
            new DirectoryNamingContexts(null, null, null, null),
            Array.Empty<CollectedObject>(),
            Array.Empty<ObjectTypeOutcome>(),
            Array.Empty<string>(),
            CollectionDepth.Quick,
            CollectorVersion,
            warnings,
            errors);

    private static LdapConnection CreateConnection(DirectoryTarget target)
    {
        var identifier = new LdapDirectoryIdentifier(target.Host, target.Port, true, false);
        var connection = new LdapConnection(identifier)
        {
            AuthType = AuthType.Negotiate,
            Timeout = target.Timeout
        };

        connection.SessionOptions.ProtocolVersion = 3;
        connection.SessionOptions.SecureSocketLayer = target.Protocol == DirectoryProtocol.Ldaps;
        connection.SessionOptions.ReferralChasing = ReferralChasingOptions.None;

        if (target.Credential is not null)
        {
            connection.Credential = new NetworkCredential(
                target.Credential.UserName,
                target.Credential.Secret,
                target.Credential.Domain);
        }

        return connection;
    }

    private static DirectoryNamingContexts ReadRootDse(LdapConnection connection, TimeSpan timeout)
    {
        var request = new SearchRequest(string.Empty, "(objectClass=*)", SearchScope.Base, RootDseAttributes);
        var response = (SearchResponse)connection.SendRequest(request, timeout);
        var entry = response.Entries.Count > 0 ? response.Entries[0] : null;
        if (entry is null)
        {
            throw new DirectoryOperationException("RootDSE nao retornou entradas.");
        }

        return new DirectoryNamingContexts(
            ReadFirst(entry, "defaultNamingContext"),
            ReadFirst(entry, "configurationNamingContext"),
            ReadFirst(entry, "schemaNamingContext"),
            ReadFirst(entry, "rootDomainNamingContext"));
    }

    private static List<SearchResultEntry> RunSearch(
        LdapConnection connection,
        string searchBase,
        string filter,
        SearchScope scope,
        string[] attributes,
        int maxObjects,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var results = new List<SearchResultEntry>();

        if (scope == SearchScope.Base)
        {
            var request = new SearchRequest(searchBase, filter, scope, attributes);
            var response = (SearchResponse)connection.SendRequest(request, timeout);
            results.AddRange(response.Entries.Cast<SearchResultEntry>());
            return results;
        }

        var pageControl = new PageResultRequestControl(PageSize);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var request = new SearchRequest(searchBase, filter, scope, attributes);
            request.Controls.Add(pageControl);

            var response = (SearchResponse)connection.SendRequest(request, timeout);

            foreach (SearchResultEntry entry in response.Entries)
            {
                results.Add(entry);
                if (results.Count >= maxObjects)
                {
                    return results;
                }
            }

            var pageResponse = response.Controls
                .OfType<PageResultResponseControl>()
                .FirstOrDefault();
            if (pageResponse is null || pageResponse.Cookie.Length == 0)
            {
                break;
            }

            pageControl.Cookie = pageResponse.Cookie;
        }

        return results;
    }

    private static IEnumerable<TypePlan> BuildPlans(IReadOnlyList<AdObjectType> types, string domainDn)
    {
        foreach (var type in types.Distinct())
        {
            switch (type)
            {
                case AdObjectType.User:
                    yield return new TypePlan(type, _ => domainDn,
                        "(&(objectCategory=person)(objectClass=user))", SearchScope.Subtree, AttributesFor(type));
                    break;
                case AdObjectType.Computer:
                    yield return new TypePlan(type, _ => domainDn,
                        "(objectCategory=computer)", SearchScope.Subtree, AttributesFor(type));
                    break;
                case AdObjectType.Group:
                    yield return new TypePlan(type, _ => domainDn,
                        "(objectCategory=group)", SearchScope.Subtree, AttributesFor(type));
                    break;
                case AdObjectType.OrganizationalUnit:
                    yield return new TypePlan(type, _ => domainDn,
                        "(objectCategory=organizationalUnit)", SearchScope.Subtree, AttributesFor(type));
                    break;
                case AdObjectType.GroupPolicyContainer:
                    yield return new TypePlan(type, root => $"CN=Policies,CN=System,{root}",
                        "(objectClass=groupPolicyContainer)", SearchScope.Subtree, AttributesFor(type));
                    break;
                case AdObjectType.Domain:
                    // tratado separadamente (objeto raiz)
                    break;
            }
        }
    }

    /// <summary>Atributos lidos por tipo (compartilhado entre coleta normal e dirigida).</summary>
    private static string[] AttributesFor(AdObjectType type) => type switch
    {
        AdObjectType.User =>
            ["sAMAccountName", "distinguishedName", "objectGUID", "objectSid", "userAccountControl",
             "lastLogonTimestamp", "pwdLastSet", "whenCreated", "whenChanged", "displayName",
             "userPrincipalName", "servicePrincipalName", "adminCount", "memberOf", "manager", "accountExpires",
             // Indicadores operacionais: lockoutTime (bloqueio) e a data de expiracao
             // efetiva da senha ja calculada pelo AD (considera politicas refinadas/PSO).
             "lockoutTime", "msDS-UserPasswordExpiryTimeComputed"],
        AdObjectType.Computer =>
            ["sAMAccountName", "distinguishedName", "objectGUID", "objectSid", "userAccountControl",
             "lastLogonTimestamp", "pwdLastSet", "whenCreated", "whenChanged", "operatingSystem",
             "operatingSystemVersion", "dNSHostName"],
        AdObjectType.Group =>
            ["sAMAccountName", "distinguishedName", "objectGUID", "objectSid", "groupType",
             "member", "adminCount", "whenCreated", "whenChanged", "description"],
        AdObjectType.OrganizationalUnit =>
            ["distinguishedName", "objectGUID", "name", "whenCreated", "whenChanged", "managedBy", "gPLink"],
        AdObjectType.GroupPolicyContainer =>
            ["distinguishedName", "objectGUID", "displayName", "gPCFileSysPath", "flags", "versionNumber",
             "whenCreated", "whenChanged"],
        AdObjectType.Domain =>
            ["ms-DS-MachineAccountQuota", "msDS-Behavior-Version", "objectGUID", "objectSid", "whenCreated", "name", "gPLink",
             // Politica de senha/bloqueio do dominio (contexto para indicadores operacionais).
             "maxPwdAge", "lockoutDuration", "lockoutThreshold"],
        _ => ["distinguishedName", "objectGUID", "objectSid", "sAMAccountName", "whenCreated", "whenChanged"]
    };

    /// <summary>
    /// Reavaliacao dirigida: le apenas os objetos indicados (busca Base por DN). Um
    /// objeto ausente (provavel remocao) vira aviso, sem mascarar a capacidade.
    /// </summary>
    private void CollectFocused(
        LdapConnection connection,
        CollectionRequest request,
        List<CollectedObject> objects,
        List<ObjectTypeOutcome> outcomes,
        List<string> warnings,
        IProgress<CollectionProgress>? progress,
        CancellationToken cancellationToken)
    {
        var perType = request.FocusObjects
            .Select(f => f.Type)
            .Distinct()
            .ToDictionary(t => t, _ => 0);

        foreach (var focus in request.FocusObjects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new CollectionProgress("Collect", focus.Type, objects.Count, $"Reavaliando {focus.DistinguishedName}"));

            try
            {
                var entries = RunSearch(
                    connection, focus.DistinguishedName, "(objectClass=*)", SearchScope.Base,
                    AttributesFor(focus.Type), 1, request.Target.Timeout, cancellationToken);
                foreach (var entry in entries)
                {
                    objects.Add(MapEntry(entry, focus.Type));
                    perType[focus.Type]++;
                }
            }
            catch (DirectoryOperationException)
            {
                // Objeto inexistente (provavel remocao). Esperado: a reconciliacao
                // classifica como removido. O tipo continua Ready (capacidade existe).
                warnings.Add($"Objeto nao encontrado (possivel remocao): {focus.DistinguishedName}");
            }
            catch (LdapException ex)
            {
                warnings.Add($"Falha ao reavaliar {focus.DistinguishedName}: {ex.Message}");
            }
        }

        foreach (var (type, count) in perType)
        {
            outcomes.Add(new ObjectTypeOutcome(type, CapabilityState.Ready, count, null));
        }
    }

    /// <summary>
    /// Enriquece o objeto de dominio com o estado da Lixeira do AD e a janela de
    /// retencao, lidos do Configuration NC. Sem permissao/erro: nao define
    /// `recycleBinEnabled` (a regra entao nao gera achado, evitando falso positivo).
    /// </summary>
    private static CollectedObject EnrichDomainWithConfig(
        LdapConnection connection,
        string? configNc,
        TimeSpan timeout,
        CollectedObject domain,
        List<string> warnings)
    {
        if (string.IsNullOrWhiteSpace(configNc))
        {
            return domain;
        }

        var extra = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var partitions = RunSearch(connection, $"CN=Partitions,{configNc}", "(objectClass=*)",
                SearchScope.Base, ["msDS-EnabledFeature"], 1, timeout, CancellationToken.None);
            var enabled = partitions.Count > 0 && partitions[0].Attributes.Contains("msDS-EnabledFeature")
                && partitions[0].Attributes["msDS-EnabledFeature"].GetValues(typeof(string)).OfType<string>()
                    .Any(v => v.Contains("Recycle Bin Feature", StringComparison.OrdinalIgnoreCase));
            extra["recycleBinEnabled"] = enabled ? "true" : "false";
        }
        catch (Exception ex) when (ex is LdapException or DirectoryOperationException)
        {
            warnings.Add($"Nao foi possivel ler o estado da Lixeira do AD: {ex.Message}");
        }

        try
        {
            var ds = RunSearch(connection, $"CN=Directory Service,CN=Windows NT,CN=Services,{configNc}",
                "(objectClass=*)", SearchScope.Base, ["tombstoneLifetime", "msDS-DeletedObjectLifetime"], 1, timeout, CancellationToken.None);
            if (ds.Count > 0)
            {
                var tombstone = ReadFirst(ds[0], "tombstoneLifetime");
                if (tombstone is not null) extra["tombstoneLifetime"] = tombstone;
                var deletedLifetime = ReadFirst(ds[0], "msDS-DeletedObjectLifetime");
                if (deletedLifetime is not null) extra["msDS-DeletedObjectLifetime"] = deletedLifetime;
            }
        }
        catch (Exception ex) when (ex is LdapException or DirectoryOperationException)
        {
            warnings.Add($"Nao foi possivel ler a retencao de objetos deletados: {ex.Message}");
        }

        try
        {
            // GPOs vinculadas a sites (Configuration NC) — para nao marcar como
            // "nao vinculada" uma GPO que so tem link de site.
            var sites = RunSearch(connection, $"CN=Sites,{configNc}", "(gPLink=*)",
                SearchScope.Subtree, ["gPLink"], 5000, timeout, CancellationToken.None);
            var siteGuids = sites
                .SelectMany(e => ReadValues(e, "gPLink"))
                .SelectMany(ExtractGpoGuids)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (siteGuids.Count > 0)
            {
                extra["siteLinkedGpoGuids"] = string.Join(",", siteGuids);
            }
        }
        catch (Exception ex) when (ex is LdapException or DirectoryOperationException)
        {
            warnings.Add($"Nao foi possivel ler links de GPO em sites: {ex.Message}");
        }

        return WithExtraAttributes(domain, extra);
    }

    /// <summary>
    /// Sinais de saude do banco do AD detectaveis por LDAP: objetos de conflito de
    /// replicacao (CNF) e objetos orfaos em LostAndFound. Anexa contagem + amostra
    /// ao objeto de dominio (atributos sinteticos lidos pelas regras de governanca).
    /// </summary>
    private static CollectedObject EnrichDomainWithHealth(
        LdapConnection connection,
        string defaultNamingContext,
        TimeSpan timeout,
        CollectedObject domain,
        List<string> warnings)
    {
        var extra = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            // Objetos de conflito de replicacao: o RDN contem o marcador "\nCNF:".
            var conflicts = RunSearch(connection, defaultNamingContext, "(cn=*\\0ACNF:*)",
                SearchScope.Subtree, ["distinguishedName"], 5000, timeout, CancellationToken.None);
            extra["conflictObjectCount"] = conflicts.Count.ToString();
            if (conflicts.Count > 0)
            {
                extra["conflictObjectSample"] = string.Join(" | ",
                    conflicts.Take(5).Select(e => e.DistinguishedName));
            }
        }
        catch (Exception ex) when (ex is LdapException or DirectoryOperationException)
        {
            warnings.Add($"Nao foi possivel verificar objetos de conflito (CNF): {ex.Message}");
        }

        try
        {
            var lostFound = RunSearch(connection, $"CN=LostAndFound,{defaultNamingContext}", "(objectClass=*)",
                SearchScope.OneLevel, ["distinguishedName"], 5000, timeout, CancellationToken.None);
            extra["lostFoundCount"] = lostFound.Count.ToString();
            if (lostFound.Count > 0)
            {
                extra["lostFoundSample"] = string.Join(" | ",
                    lostFound.Take(5).Select(e => e.DistinguishedName));
            }
        }
        catch (Exception ex) when (ex is LdapException or DirectoryOperationException)
        {
            warnings.Add($"Nao foi possivel verificar o LostAndFound: {ex.Message}");
        }

        return WithExtraAttributes(domain, extra);
    }

    /// <summary>Le todos os valores (string) de um atributo de uma entrada.</summary>
    private static IReadOnlyList<string> ReadValues(SearchResultEntry entry, string name) =>
        entry.Attributes.Contains(name)
            ? entry.Attributes[name].GetValues(typeof(string)).OfType<string>().ToList()
            : Array.Empty<string>();

    /// <summary>
    /// Extrai os GUIDs de GPO de um valor gPLink, ex.:
    /// "[LDAP://cn={GUID},cn=policies,cn=system,DC=...;0]". Normaliza para minusculo sem chaves.
    /// </summary>
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

            start += 3; // posiciona no "{"
            var end = token.IndexOf('}', start);
            if (end > start)
            {
                yield return token.Substring(start, end - start + 1).Trim('{', '}').ToLowerInvariant();
            }
        }
    }

    private static CollectedObject WithExtraAttributes(CollectedObject obj, IReadOnlyDictionary<string, string> extra)
    {
        if (extra.Count == 0)
        {
            return obj;
        }

        var dict = new Dictionary<string, IReadOnlyList<string>>(obj.Attributes, StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in extra)
        {
            dict[key] = [value];
        }

        return obj with { Attributes = dict };
    }

    /// <summary>
    /// Le os GUIDs de objetos na Lixeira do AD (CN=Deleted Objects) usando o control
    /// Show Deleted Objects (OID 1.2.840.113556.1.4.417), paginado.
    /// </summary>
    private static List<string> CollectDeletedObjectGuids(
        LdapConnection connection,
        string defaultNamingContext,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var guids = new List<string>();
        var baseDn = $"CN=Deleted Objects,{defaultNamingContext}";
        var pageControl = new PageResultRequestControl(PageSize);
        var showDeleted = new DirectoryControl("1.2.840.113556.1.4.417", null, true, true);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var request = new SearchRequest(baseDn, "(isDeleted=TRUE)", SearchScope.Subtree, "objectGUID");
            request.Controls.Add(pageControl);
            request.Controls.Add(showDeleted);

            var response = (SearchResponse)connection.SendRequest(request, timeout);
            foreach (SearchResultEntry entry in response.Entries)
            {
                if (entry.Attributes.Contains("objectGUID"))
                {
                    var guid = ReadGuid(entry.Attributes["objectGUID"]);
                    if (guid is not null)
                    {
                        guids.Add(guid);
                    }
                }
            }

            var pageResponse = response.Controls.OfType<PageResultResponseControl>().FirstOrDefault();
            if (pageResponse is null || pageResponse.Cookie.Length == 0)
            {
                break;
            }

            pageControl.Cookie = pageResponse.Cookie;
        }

        return guids;
    }

    private static CollectedObject MapEntry(SearchResultEntry entry, AdObjectType type)
    {
        var attributes = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        string? objectGuid = null;
        string? objectSid = null;

        foreach (DirectoryAttribute attribute in entry.Attributes.Values)
        {
            var name = attribute.Name;
            if (string.Equals(name, "objectGUID", StringComparison.OrdinalIgnoreCase))
            {
                objectGuid = ReadGuid(attribute);
                continue;
            }

            if (string.Equals(name, "objectSid", StringComparison.OrdinalIgnoreCase))
            {
                objectSid = ReadSid(attribute);
                continue;
            }

            var values = attribute.GetValues(typeof(string))
                .OfType<string>()
                .ToList();
            attributes[name] = values;
        }

        var dn = entry.DistinguishedName;
        var sam = attributes.TryGetValue("sAMAccountName", out var samValues) && samValues.Count > 0
            ? samValues[0]
            : null;

        return new CollectedObject(type, dn, objectGuid, objectSid, sam, attributes);
    }

    private static string? ReadGuid(DirectoryAttribute attribute)
    {
        var bytes = attribute.GetValues(typeof(byte[])).OfType<byte[]>().FirstOrDefault();
        return bytes is { Length: 16 } ? new Guid(bytes).ToString() : null;
    }

    private static string? ReadSid(DirectoryAttribute attribute)
    {
        var bytes = attribute.GetValues(typeof(byte[])).OfType<byte[]>().FirstOrDefault();
        if (bytes is null || bytes.Length == 0)
        {
            return null;
        }

        try
        {
            return new SecurityIdentifier(bytes, 0).Value;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static string? ReadFirst(SearchResultEntry entry, string name)
    {
        if (!entry.Attributes.Contains(name))
        {
            return null;
        }

        var attribute = entry.Attributes[name];
        return attribute is { Count: > 0 } ? attribute[0]?.ToString() : null;
    }
}
