using Direnix.Core.Audit;
using Direnix.Core.Collection;
using Direnix.Core.Rules;
using Direnix.Core.Scheduling;
using Direnix.Core.Storage;

namespace Direnix.Service.Collection;

/// <summary>
/// Dispara a coleta automática no horário configurado, autenticando como a
/// identidade do serviço (gMSA) — sem credencial armazenada. Canal endurecido:
/// LDAPS + Negotiate (Kerberos sign+seal).
/// </summary>
public sealed class ScheduledCollectionService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);

    private readonly IProductStore store;
    private readonly CollectionJobService jobs;
    private readonly ILogger<ScheduledCollectionService> logger;

    public ScheduledCollectionService(IProductStore store, CollectionJobService jobs, ILogger<ScheduledCollectionService> logger)
    {
        this.store = store;
        this.jobs = jobs;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha no agendador de coleta.");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task TickAsync(CancellationToken cancellationToken)
    {
        var config = await store.GetScheduleConfigAsync(cancellationToken);
        var now = DateTimeOffset.Now;
        if (!config.Enabled || string.IsNullOrWhiteSpace(config.Host) || config.NextRunAt is null || config.NextRunAt > now)
        {
            return;
        }

        logger.LogInformation("Agendamento disparando coleta automatica em {Host}.", config.Host);

        // Perfil explícito do agendamento; se vazio/inexistente, usa o perfil ativo.
        var profiles = await store.GetRuleProfilesAsync(cancellationToken);
        var profile = (!string.IsNullOrWhiteSpace(config.ProfileName)
            ? profiles.Profiles.FirstOrDefault(p => string.Equals(p.Name, config.ProfileName, StringComparison.OrdinalIgnoreCase))
            : null) ?? profiles.ResolveActive();
        var target = new DirectoryTarget(config.Host.Trim(), DirectoryProtocol.Ldaps, 636, null, TimeSpan.FromSeconds(30));
        var request = new CollectionRequest(
            target,
            RequiredObjectTypesFor(profile),
            ["Inventory", "CleanupHygiene", "PrivilegedAccess"],
            CollectionDepth.Standard,
            Operator: "scheduler")
        {
            CustomIndicators = Direnix.Core.Indicators.CustomIndicatorResolver.Resolve(profile)
        };

        jobs.StartJob(request);

        await store.AppendAuditAsync(new AuditEvent(
            DateTimeOffset.UtcNow,
            $"{Environment.UserDomainName}\\{Environment.UserName}",
            "scheduler",
            "AssessmentStarted",
            "Directory",
            config.Host,
            "127.0.0.1",
            Environment.MachineName,
            "Success",
            new Dictionary<string, string> { ["trigger"] = "schedule", ["frequency"] = config.Frequency.ToString(), ["profile"] = profile.Name }),
            cancellationToken);

        var next = ScheduleCalculator.Next(config, now.AddMinutes(1));
        await store.SaveScheduleConfigAsync(config with { LastRunAt = now, NextRunAt = next }, cancellationToken);
    }

    // Escopo "responsivo": coleta os tipos exigidos pelas regras habilitadas no perfil ativo.
    private static IReadOnlyList<AdObjectType> RequiredObjectTypesFor(RuleProfile profile)
    {
        var types = RuleCatalog.All
            .Where(d => profile.IsRuleEnabled(d.RuleId))
            .SelectMany(d => RuleCatalog.RequiredObjectTypes(d.RuleId))
            .Where(t => t != AdObjectType.Domain)
            .Distinct()
            .ToList();

        return types.Count > 0
            ? types
            : [AdObjectType.User, AdObjectType.Computer, AdObjectType.Group, AdObjectType.OrganizationalUnit, AdObjectType.GroupPolicyContainer];
    }
}
