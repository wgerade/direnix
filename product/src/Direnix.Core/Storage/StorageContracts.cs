namespace Direnix.Core.Storage;

public sealed record DatabaseKeyMaterial(byte[] Key, string ProtectionMode) : IDisposable
{
    public void Dispose() => Array.Clear(Key, 0, Key.Length);
}

public sealed record ProductStorageHealth(
    bool IsConfigured,
    bool KeyAvailable,
    bool SchemaAvailable,
    int SchemaVersion,
    string ProtectionMode,
    string DatabasePath,
    string Message);

public interface IDatabaseKeyStore
{
    ValueTask<DatabaseKeyMaterial> GetOrCreateAsync(CancellationToken cancellationToken);
}

public interface IProductStore
{
    Task<ProductStorageHealth> CheckHealthAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Persiste um run completo (run + inventario + metricas + findings) numa
    /// unica transacao, reconciliando a timeline de findings por
    /// <c>stableFindingKey</c> (New/Open/Recurring/Resolved).
    /// </summary>
    Task SaveRunAsync(
        Collection.CollectionResult result,
        IReadOnlyList<Findings.Finding> findings,
        IReadOnlyList<MetricValue> metrics,
        IReadOnlyList<string> evaluatedRuleIds,
        IReadOnlyList<Indicators.IndicatorResult> indicators,
        RunMetadata metadata,
        CancellationToken cancellationToken);

    /// <summary>Indicadores operacionais do run mais recente (para os cards do painel).</summary>
    Task<IReadOnlyList<IndicatorResultRow>> GetLatestIndicatorsAsync(CancellationToken cancellationToken);

    Task<RunRecord?> GetLatestRunAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<RunSummary>> GetRunsAsync(int limit, CancellationToken cancellationToken);

    Task<IReadOnlyList<ObjectHistoryEntry>> GetObjectHistoryAsync(string objectKey, CancellationToken cancellationToken);

    Task<IReadOnlyList<RiskExceptionView>> GetExceptionViewsAsync(bool includeExpired, CancellationToken cancellationToken);

    Task<DashboardData> GetDashboardAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<FindingRow>> GetFindingsAsync(
        string view,
        string? category,
        int limit,
        int offset,
        CancellationToken cancellationToken);

    Task<FindingRow?> GetFindingAsync(string stableFindingKey, CancellationToken cancellationToken);

    Task<IReadOnlyList<InventoryState>> GetCurrentInventoryAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Resolve chaves de objeto (<c>objectKey</c>) em DN + tipo, a partir do estado
    /// atual conhecido, para uma reavaliacao dirigida (Reavaliar selecionados).
    /// </summary>
    Task<IReadOnlyList<Collection.FocusObject>> GetObjectsForRefreshAsync(
        IReadOnlyCollection<string> objectKeys,
        CancellationToken cancellationToken);

    Task<Rules.RuleProfilesState> GetRuleProfilesAsync(CancellationToken cancellationToken);

    Task SaveRuleProfilesAsync(Rules.RuleProfilesState state, CancellationToken cancellationToken);

    Task<RiskExceptionRecord> AddExceptionAsync(RiskExceptionInput input, CancellationToken cancellationToken);

    Task<IReadOnlyList<RiskExceptionRecord>> GetExceptionsAsync(bool includeExpired, CancellationToken cancellationToken);

    Task RemoveExceptionAsync(string exceptionId, CancellationToken cancellationToken);

    /// <summary>Registra um evento de auditoria (acao executada no portal).</summary>
    Task AppendAuditAsync(Audit.AuditEvent auditEvent, CancellationToken cancellationToken);

    /// <summary>Eventos de auditoria mais recentes (ordem decrescente por data).</summary>
    Task<IReadOnlyList<Audit.AuditEvent>> GetAuditEventsAsync(int limit, CancellationToken cancellationToken);

    /// <summary>Eventos de mudança recentes (Timeline). Filtra por período/tipo opcionais.</summary>
    Task<IReadOnlyList<ChangeEventRow>> GetChangesAsync(int limit, int? sinceHours, string? changeType, CancellationToken cancellationToken);

    /// <summary>Contagem de mudanças por tipo no período (Morning View).</summary>
    Task<IReadOnlyList<ChangeCount>> GetChangeSummaryAsync(int sinceHours, CancellationToken cancellationToken);

    /// <summary>
    /// Quantidade de achados NOVOS (primeira observação dentro do período) que ainda
    /// estão ATIVOS — exclui os já resolvidos / aceitos. Alimenta o card de riscos
    /// novos no "o que mudou nas últimas 24h".
    /// </summary>
    Task<int> CountNewFindingsAsync(int sinceHours, CancellationToken cancellationToken);

    /// <summary>Busca global por objeto (sam, nome, DN, SID, GUID, UPN, SPN).</summary>
    Task<IReadOnlyList<SearchHit>> SearchObjectsAsync(string query, int limit, CancellationToken cancellationToken);

    /// <summary>Visão consolidada de um objeto: atributos + findings + timeline.</summary>
    Task<ObjectDetail?> GetObjectDetailAsync(string objectKey, CancellationToken cancellationToken);

    // --- Login local (Bloco B-min) ---
    Task<int> GetUserCountAsync(CancellationToken cancellationToken);
    Task CreateUserAsync(Auth.AppUserRecord user, CancellationToken cancellationToken);
    Task<Auth.AppUserRecord?> GetUserByNameAsync(string username, CancellationToken cancellationToken);
    Task CreateSessionAsync(Auth.AppSession session, CancellationToken cancellationToken);
    Task<Auth.AppSession?> GetSessionAsync(string token, CancellationToken cancellationToken);
    Task DeleteSessionAsync(string token, CancellationToken cancellationToken);

    /// <summary>
    /// Reset seguro do login: remove TODOS os usuarios e sessoes (reabre a tela
    /// "Criar administrador"). NAO toca em coletas/perfis/indicadores. Retorna
    /// quantos usuarios foram removidos. Usado pelo modo CLI --reset-admin.
    /// </summary>
    Task<int> ResetAdminAsync(CancellationToken cancellationToken);

    // --- Agendamento da coleta automática ---
    Task<Scheduling.ScheduleConfig> GetScheduleConfigAsync(CancellationToken cancellationToken);
    Task SaveScheduleConfigAsync(Scheduling.ScheduleConfig config, CancellationToken cancellationToken);

    // --- Configurações genéricas (KV app_settings): notificações, último resultado, etc. ---
    Task<string?> GetSettingAsync(string key, CancellationToken cancellationToken);
    Task SetSettingAsync(string key, string value, CancellationToken cancellationToken);
}

public interface ISchemaMigrator
{
    Task<int> GetCurrentVersionAsync(CancellationToken cancellationToken);

    Task MigrateAsync(CancellationToken cancellationToken);
}
