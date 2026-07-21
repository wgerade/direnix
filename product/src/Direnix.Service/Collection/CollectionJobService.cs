using Direnix.Core.Collection;
using Direnix.Core.Findings;
using Direnix.Core.Indicators;
using Direnix.Core.Rules;
using Direnix.Core.Storage;

namespace Direnix.Service.Collection;

public enum JobStatus
{
    Queued,
    Running,
    Completed,
    Failed
}

/// <summary>Snapshot imutavel do estado de um job para a API.</summary>
public sealed record JobSnapshot(
    string JobId,
    JobStatus Status,
    string Stage,
    int CollectedSoFar,
    string Message,
    string? RunId,
    string? CoverageMode,
    int FindingCount,
    string? Error,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt);

/// <summary>
/// Orquestra a coleta read-only de ponta a ponta: motor LDAP -> regras de higiene
/// -> persistencia. Mantem o estado dos jobs em memoria (instancia unica do
/// Windows Service).
/// </summary>
public sealed class CollectionJobService
{
    private static readonly HashSet<string> StaleRuleIds =
        ["ADCLN-USER-STALE-001", "ADCLN-COMP-STALE-003"];
    private const string PrivilegedRuleId = "ADPRV-T0-GROUPS-001";

    private readonly ICollectionEngine engine;
    private readonly HygieneRuleEngine ruleEngine;
    private readonly IProductStore store;
    private readonly ILogger<CollectionJobService> logger;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, JobState> jobs = new();

    public CollectionJobService(
        ICollectionEngine engine,
        HygieneRuleEngine ruleEngine,
        IProductStore store,
        ILogger<CollectionJobService> logger)
    {
        this.engine = engine;
        this.ruleEngine = ruleEngine;
        this.store = store;
        this.logger = logger;
    }

    public string StartJob(CollectionRequest request, Func<JobSnapshot, Task>? onCompleted = null)
    {
        var jobId = Guid.NewGuid().ToString("N");
        var state = new JobState(jobId);
        jobs[jobId] = state;

        _ = Task.Run(() => RunJobAsync(state, request, onCompleted));
        return jobId;
    }

    public JobSnapshot? GetJob(string jobId) =>
        jobs.TryGetValue(jobId, out var state) ? state.Snapshot() : null;

    private async Task RunJobAsync(JobState state, CollectionRequest request, Func<JobSnapshot, Task>? onCompleted = null)
    {
        state.SetRunning();
        var progress = new Progress<CollectionProgress>(report =>
            state.UpdateProgress(report.Stage, report.CollectedSoFar, report.Message));

        try
        {
            var result = await engine.CollectAsync(request, progress, CancellationToken.None);

            // Reavaliacao dirigida: fixa o escopo da reconciliacao nos objetos
            // selecionados (mesmo os removidos, que nao voltam em result.Objects).
            if (request.IsFocused)
            {
                result = result with { ScopeObjectKeys = request.FocusObjects.Select(f => f.ObjectKey).ToList() };
            }

            // RootDSE/base inacessivel -> tratado como falha (doc: Blocked), sem
            // persistir um run vazio.
            if (result.CoverageMode == CoverageMode.NoDirectory)
            {
                var reason = result.Errors.Count > 0
                    ? string.Join(" ", result.Errors)
                    : "Nao foi possivel acessar o diretorio (RootDSE/base indisponivel).";
                state.Fail(reason);
                logger.LogWarning("Run {JobId} bloqueado: {Reason}", state.JobId, reason);
                return;
            }

            state.UpdateProgress("Evaluate", result.Objects.Count, "Avaliando regras de higiene");

            var profiles = await store.GetRuleProfilesAsync(CancellationToken.None);
            var profile = profiles.ResolveActive();
            var findings = ruleEngine.Evaluate(result, profile);
            var metrics = BuildMetrics(findings);

            // Indicadores operacionais (senhas vencendo/expiradas, contas bloqueadas,
            // customizados do perfil). Nao geram risco — sao acompanhamento do dia a dia.
            var indicators = IndicatorEngine.Evaluate(result, profile);

            // Reconciliacao com escopo: so podem ser resolvidas as regras cujos tipos
            // de objeto foram coletados neste run (Ready). O resto fica carried-forward.
            var collected = result.Outcomes
                .Where(o => o.State == CapabilityState.Ready)
                .Select(o => o.ObjectType)
                .ToHashSet();
            if (result.Objects.Any(o => o.ObjectType == AdObjectType.Domain))
            {
                collected.Add(AdObjectType.Domain);
            }
            var evaluatedRuleIds = ruleEngine.RuleIds
                .Where(id => profile.IsRuleEnabled(id))
                .Where(id => RuleCatalog.RequiredObjectTypes(id).All(collected.Contains))
                .ToList();

            var metadata = new RunMetadata(
                string.IsNullOrWhiteSpace(request.Operator) ? null : request.Operator.Trim(),
                $"{Environment.UserDomainName}\\{Environment.UserName}",
                request.Target.Credential?.Principal ?? "current-context");

            await store.SaveRunAsync(result, findings, metrics, evaluatedRuleIds, indicators, metadata, CancellationToken.None);

            state.Complete(result.RunId, result.CoverageMode.ToString(), findings.Count);
            logger.LogInformation(
                "Run {RunId} concluido: {Objects} objetos, {Findings} findings, cobertura {Coverage}.",
                result.RunId, result.Objects.Count, findings.Count, result.CoverageMode);
        }
        catch (Exception ex)
        {
            state.Fail(ex.Message);
            logger.LogError(ex, "Falha no run de coleta {JobId}.", state.JobId);
        }
        finally
        {
            if (onCompleted is not null)
            {
                try { await onCompleted(state.Snapshot()); }
                catch (Exception ex) { logger.LogWarning(ex, "Callback pos-coleta falhou no job {JobId}.", state.JobId); }
            }
        }
    }

    private static IReadOnlyList<MetricValue> BuildMetrics(IReadOnlyList<Finding> findings)
    {
        var riskScore = findings.Count == 0 ? 0 : findings.Max(f => f.BusinessRiskScore);
        var staleObjects = findings.Count(f => StaleRuleIds.Contains(f.RuleId));
        var privilegedExposure = findings.Count(f => f.RuleId == PrivilegedRuleId);

        return
        [
            new MetricValue("riskScore", riskScore),
            new MetricValue("findings", findings.Count),
            new MetricValue("staleObjects", staleObjects),
            new MetricValue("privilegedExposure", privilegedExposure)
        ];
    }

    private sealed class JobState
    {
        private readonly object gate = new();
        private JobStatus status = JobStatus.Queued;
        private string stage = "Queued";
        private int collected;
        private string message = "Job enfileirado";
        private string? runId;
        private string? coverageMode;
        private int findingCount;
        private string? error;
        private readonly DateTimeOffset startedAt = DateTimeOffset.UtcNow;
        private DateTimeOffset? completedAt;

        public JobState(string jobId) => JobId = jobId;

        public string JobId { get; }

        public void SetRunning()
        {
            lock (gate)
            {
                status = JobStatus.Running;
                stage = "Connecting";
                message = "Conectando ao diretorio";
            }
        }

        public void UpdateProgress(string newStage, int collectedSoFar, string newMessage)
        {
            lock (gate)
            {
                stage = newStage;
                collected = collectedSoFar;
                message = newMessage;
            }
        }

        public void Complete(string completedRunId, string coverage, int findings)
        {
            lock (gate)
            {
                status = JobStatus.Completed;
                stage = "Completed";
                message = "Avaliacao concluida";
                runId = completedRunId;
                coverageMode = coverage;
                findingCount = findings;
                completedAt = DateTimeOffset.UtcNow;
            }
        }

        public void Fail(string reason)
        {
            lock (gate)
            {
                status = JobStatus.Failed;
                stage = "Failed";
                message = "Falha na avaliacao";
                error = reason;
                completedAt = DateTimeOffset.UtcNow;
            }
        }

        public JobSnapshot Snapshot()
        {
            lock (gate)
            {
                return new JobSnapshot(
                    JobId, status, stage, collected, message, runId,
                    coverageMode, findingCount, error, startedAt, completedAt);
            }
        }
    }
}
