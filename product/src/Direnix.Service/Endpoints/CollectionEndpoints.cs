using Direnix.Core.Collection;
using Direnix.Core.Storage;
using Direnix.Service.Collection;
using DirectoryProtocol = Direnix.Core.Collection.DirectoryProtocol;

namespace Direnix.Service.Endpoints;

public static class CollectionEndpoints
{
    public static IEndpointRouteBuilder MapCollectionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/collection");

        group.MapPost("/probe", async (
            DirectoryProbeRequest request,
            IAdDirectoryProbe probe,
            CancellationToken cancellationToken) =>
        {
            var validation = ValidateRequest(request.Host, request.Protocol, request.Port, request.UserName, request.Secret);
            if (validation is not null)
            {
                return Results.BadRequest(validation);
            }

            var target = BuildTarget(request.Host, request.Protocol, request.Port, request.Domain, request.UserName, request.Secret, request.TimeoutSeconds);
            var result = await probe.ProbeRootDseAsync(target, cancellationToken);

            return Results.Ok(new
            {
                state = result.State.ToString(),
                result.Host,
                Protocol = result.Protocol.ToString(),
                result.Port,
                result.NamingContexts,
                result.Warnings,
                result.Errors,
                credentialMode = target.Credential is null ? "current-context" : "explicit-request-scope"
            });
        });

        group.MapPost("/runs", async (
            CollectionRunRequest request,
            CollectionJobService jobs,
            IProductStore store,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var validation = ValidateRequest(request.Host, request.Protocol, request.Port, request.UserName, request.Secret);
            if (validation is not null)
            {
                return Results.BadRequest(validation);
            }

            var target = BuildTarget(request.Host, request.Protocol, request.Port, request.Domain, request.UserName, request.Secret, request.TimeoutSeconds);

            var oper = string.IsNullOrWhiteSpace(request.Operator) ? null : request.Operator.Trim();
            var maxPerType = Math.Clamp(request.MaxObjectsPerType ?? 50_000, 100, 200_000);

            CollectionRequest collectionRequest;
            if (request.FocusObjectKeys is { Count: > 0 })
            {
                // Reavaliacao dirigida: le apenas os objetos selecionados. O escopo do
                // perfil e ignorado; a reconciliacao fica restrita a esses objetos.
                var focus = await store.GetObjectsForRefreshAsync(request.FocusObjectKeys, cancellationToken);
                if (focus.Count == 0)
                {
                    return Results.BadRequest(new { error = "Nenhum dos objetos selecionados foi encontrado para reavaliacao." });
                }

                collectionRequest = new CollectionRequest(
                    target,
                    focus.Select(f => f.Type).Distinct().ToList(),
                    ["Inventory", "CleanupHygiene", "PrivilegedAccess"],
                    CollectionDepth.Standard,
                    MaxObjectsPerType: maxPerType,
                    Operator: oper)
                {
                    FocusObjects = focus
                };
            }
            else
            {
                // O escopo (tipos de objeto + profundidade) vem do PERFIL ativo, nao do
                // request: a configuracao fica num lugar so (Configuracoes).
                var profile = (await store.GetRuleProfilesAsync(cancellationToken)).ResolveActive();
                collectionRequest = new CollectionRequest(
                    target,
                    RequiredObjectTypesFor(profile),
                    ["Inventory", "CleanupHygiene", "PrivilegedAccess"],
                    CollectionDepth.Standard,
                    MaxObjectsPerType: maxPerType,
                    Operator: oper)
                {
                    CustomIndicators = Direnix.Core.Indicators.CustomIndicatorResolver.Resolve(profile)
                };
            }

            var jobId = jobs.StartJob(collectionRequest);
            var snapshot = jobs.GetJob(jobId)!;
            await PortalAudit.LogAsync(store, http, "AssessmentStarted", "Directory", request.Host, "Success",
                new Dictionary<string, string>
                {
                    ["operator"] = oper ?? "(nao informado)",
                    ["jobId"] = jobId,
                    ["focused"] = (request.FocusObjectKeys is { Count: > 0 }) ? "true" : "false"
                });
            return Results.Accepted($"/api/v1/collection/runs/{jobId}", snapshot);
        });

        group.MapGet("/runs/{jobId}", (string jobId, CollectionJobService jobs) =>
        {
            var snapshot = jobs.GetJob(jobId);
            return snapshot is null ? Results.NotFound() : Results.Ok(snapshot);
        });

        // Diagnostico: coleta uma amostra pequena e devolve os atributos crus lidos
        // do AD, para conferir o que as regras realmente recebem. Read-only.
        group.MapGet("/sample", async (
            string host,
            string? protocol,
            int? port,
            string? objectType,
            int? count,
            ICollectionEngine engine,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                return Results.BadRequest(new { error = "Informe ?host=<dc>" });
            }

            var target = BuildTarget(host, protocol, port, null, null, null, 30);
            var type = Enum.TryParse<AdObjectType>(objectType, ignoreCase: true, out var parsed) ? parsed : AdObjectType.User;
            var take = Math.Clamp(count ?? 3, 1, 25);

            var request = new CollectionRequest(
                target,
                [type],
                ["Diagnostic"],
                CollectionDepth.Quick,
                MaxObjectsPerType: take);

            var result = await engine.CollectAsync(request, null, cancellationToken);

            return Results.Ok(new
            {
                domainDn = result.DomainDn,
                domainName = result.DomainName,
                coverage = result.CoverageMode.ToString(),
                result.Warnings,
                result.Errors,
                outcomes = result.Outcomes.Select(o => new { type = o.ObjectType.ToString(), state = o.State.ToString(), o.Count, o.Message }),
                objects = result.Objects.Where(o => o.ObjectType == type).Take(take).Select(o => new
                {
                    objectType = o.ObjectType.ToString(),
                    o.DistinguishedName,
                    o.ObjectGuid,
                    o.ObjectSid,
                    o.SamAccountName,
                    attributeKeys = o.Attributes.Keys,
                    attributes = o.Attributes
                })
            });
        });

        // Diagnostico: roda coleta + regras e devolve POR QUE houve (ou nao) achados.
        group.MapGet("/analyze", async (
            string host,
            string? protocol,
            int? port,
            int? max,
            ICollectionEngine engine,
            Direnix.Core.Rules.HygieneRuleEngine ruleEngine,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                return Results.BadRequest(new { error = "Informe ?host=<dc>" });
            }

            int[] privilegedRids = [512, 518, 519, 520, 544, 548, 549, 550, 551];
            var target = BuildTarget(host, protocol, port, null, null, null, 60);
            var request = new CollectionRequest(
                target,
                [AdObjectType.User, AdObjectType.Computer, AdObjectType.Group, AdObjectType.OrganizationalUnit, AdObjectType.GroupPolicyContainer],
                ["Diagnostic"],
                CollectionDepth.Standard,
                MaxObjectsPerType: Math.Clamp(max ?? 3000, 100, 50_000));

            var result = await engine.CollectAsync(request, null, cancellationToken);
            var findings = ruleEngine.Evaluate(result);

            var domain = result.Objects.FirstOrDefault(o => o.ObjectType == AdObjectType.Domain);
            var privGroups = result.Objects
                .Where(o => o.ObjectType == AdObjectType.Group)
                .Select(g => new { g, rid = AdAttributes.RelativeId(g.ObjectSid) })
                .Where(x => x.rid is not null && privilegedRids.Contains(x.rid.Value))
                .Select(x => new
                {
                    name = x.g.SamAccountName ?? x.g.DistinguishedName,
                    sid = x.g.ObjectSid,
                    rid = x.rid,
                    memberCount = x.g.Values("member").Count
                })
                .ToList();

            return Results.Ok(new
            {
                domainName = result.DomainName,
                coverage = result.CoverageMode.ToString(),
                counts = result.Outcomes.Select(o => new { type = o.ObjectType.ToString(), state = o.State.ToString(), o.Count }),
                domainObjectFound = domain is not null,
                machineAccountQuota = domain?.Value("ms-DS-MachineAccountQuota"),
                groupsCollected = result.Objects.Count(o => o.ObjectType == AdObjectType.Group),
                privilegedGroupsDetected = privGroups,
                totalFindings = findings.Count,
                findingsByRule = findings.GroupBy(f => f.RuleId).ToDictionary(g => g.Key, g => g.Count()),
                sampleFindings = findings.Take(15).Select(f => new
                {
                    f.RuleId,
                    f.Title,
                    severity = f.Severity.ToString(),
                    f.BusinessRiskScore,
                    f.ObjectDisplay
                }),
                result.Warnings,
                result.Errors
            });
        });

        return endpoints;
    }

    private static DirectoryTarget BuildTarget(
        string host,
        string? protocol,
        int? port,
        string? domain,
        string? userName,
        string? secret,
        int? timeoutSeconds)
    {
        var parsedProtocol = ParseProtocol(protocol);
        var resolvedPort = port ?? (int)parsedProtocol;
        var timeout = TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds ?? 15, 2, 120));

        var credential = string.IsNullOrWhiteSpace(userName)
            ? null
            : new DirectoryCredential(
                userName.Trim(),
                string.IsNullOrWhiteSpace(domain) ? null : domain.Trim(),
                secret ?? string.Empty);

        return new DirectoryTarget(host.Trim(), parsedProtocol, resolvedPort, credential, timeout);
    }

    private static object? ValidateRequest(string? host, string? protocol, int? port, string? userName, string? secret)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(host))
        {
            errors["host"] = ["Informe o dominio ou controlador de dominio."];
        }

        if (!string.IsNullOrWhiteSpace(protocol) &&
            !Enum.TryParse<DirectoryProtocol>(protocol, ignoreCase: true, out _))
        {
            errors["protocol"] = ["Use Ldaps ou Ldap."];
        }

        if (port is < 1 or > 65535)
        {
            errors["port"] = ["A porta deve estar entre 1 e 65535."];
        }

        if (!string.IsNullOrWhiteSpace(userName) && string.IsNullOrEmpty(secret))
        {
            errors["secret"] = ["Informe a senha para credencial explicita ou deixe usuario em branco."];
        }

        return errors.Count == 0 ? null : new { errors };
    }

    private static DirectoryProtocol ParseProtocol(string? protocol) =>
        Enum.TryParse<DirectoryProtocol>(protocol, ignoreCase: true, out var parsed)
            ? parsed
            : DirectoryProtocol.Ldaps;

    /// <summary>
    /// Tipos de objeto a coletar = uniao dos tipos exigidos pelas regras HABILITADAS
    /// no perfil ativo (escopo "responsivo": coleta o que as regras precisam). O
    /// objeto de dominio e lido separadamente pelo coletor.
    /// </summary>
    private static IReadOnlyList<AdObjectType> RequiredObjectTypesFor(Direnix.Core.Rules.RuleProfile profile)
    {
        var types = Direnix.Core.Rules.RuleCatalog.All
            .Where(d => profile.IsRuleEnabled(d.RuleId))
            .SelectMany(d => Direnix.Core.Rules.RuleCatalog.RequiredObjectTypes(d.RuleId))
            .Where(t => t != AdObjectType.Domain)
            .Distinct()
            .ToList();

        return types.Count > 0
            ? types
            : [AdObjectType.User, AdObjectType.Computer, AdObjectType.Group, AdObjectType.OrganizationalUnit, AdObjectType.GroupPolicyContainer];
    }

}

public sealed record DirectoryProbeRequest(
    string Host,
    string? Protocol,
    int? Port,
    string? Domain,
    string? UserName,
    string? Secret,
    int? TimeoutSeconds);

public sealed record CollectionRunRequest(
    string Host,
    string? Protocol,
    int? Port,
    string? Domain,
    string? UserName,
    string? Secret,
    int? TimeoutSeconds,
    IReadOnlyList<string>? ObjectTypes,
    string? Depth,
    int? MaxObjectsPerType,
    string? Operator,
    IReadOnlyList<string>? FocusObjectKeys);
