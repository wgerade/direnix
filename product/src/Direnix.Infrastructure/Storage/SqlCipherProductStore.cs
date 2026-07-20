using System.Globalization;
using System.Text.Json;
using Direnix.Core.Audit;
using Direnix.Core.Auth;
using Direnix.Core.Changes;
using Direnix.Core.Collection;
using Direnix.Core.Scheduling;
using Direnix.Core.Scoring;
using Direnix.Core.Findings;
using Direnix.Core.Identity;
using Direnix.Core.Indicators;
using Direnix.Core.Rules;
using Direnix.Core.Storage;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace Direnix.Infrastructure.Storage;

public sealed class SqlCipherProductStore : IProductStore, ISchemaMigrator
{
    private const int CurrentSchemaVersion = 8;
    private static readonly string[] ActiveStatuses = ["New", "Open", "Recurring"];
    private const string RuleProfilesKey = "rules.profiles";
    private readonly IDatabaseKeyStore keyStore;
    private readonly ProductStorageOptions options;

    public SqlCipherProductStore(IDatabaseKeyStore keyStore, ProductStorageOptions options)
    {
        Batteries_V2.Init();
        this.keyStore = keyStore;
        this.options = options;
    }

    public async Task<ProductStorageHealth> CheckHealthAsync(CancellationToken cancellationToken)
    {
        var currentVersion = await GetCurrentVersionAsync(cancellationToken);

        return new ProductStorageHealth(
            IsConfigured: true,
            KeyAvailable: true,
            SchemaAvailable: currentVersion >= CurrentSchemaVersion,
            SchemaVersion: currentVersion,
            ProtectionMode: "DPAPI LocalMachine + SQLCipher",
            DatabasePath: options.DatabasePath,
            Message: currentVersion >= CurrentSchemaVersion
                ? "Encrypted database is ready."
                : "Encrypted database schema is not current.");
    }

    public async Task<int> GetCurrentVersionAsync(CancellationToken cancellationToken)
    {
        using var connection = await OpenConnectionAsync(cancellationToken);
        return await GetCurrentVersionAsync(connection, cancellationToken);
    }

    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        System.IO.Directory.CreateDirectory(options.DataRoot);

        using var connection = await OpenConnectionAsync(cancellationToken);
        await ExecuteNonQueryAsync(connection, "PRAGMA foreign_keys = ON;", cancellationToken);
        await ExecuteNonQueryAsync(connection, "PRAGMA journal_mode = WAL;", cancellationToken);

        var currentVersion = await GetCurrentVersionAsync(connection, cancellationToken);
        if (currentVersion >= CurrentSchemaVersion)
        {
            return;
        }

        using var transaction = connection.BeginTransaction();
        if (currentVersion < 1)
        {
            await ApplySchemaV1Async(connection, transaction, cancellationToken);
        }

        if (currentVersion < 2)
        {
            await ApplySchemaV2Async(connection, transaction, cancellationToken);
        }

        if (currentVersion < 3)
        {
            await ApplySchemaV3Async(connection, transaction, cancellationToken);
        }

        if (currentVersion < 4)
        {
            await ApplySchemaV4Async(connection, transaction, cancellationToken);
        }

        if (currentVersion < 5)
        {
            await ApplySchemaV5Async(connection, transaction, cancellationToken);
        }

        if (currentVersion < 6)
        {
            await ApplySchemaV6Async(connection, transaction, cancellationToken);
        }

        if (currentVersion < 7)
        {
            await ApplySchemaV7Async(connection, transaction, cancellationToken);
        }

        if (currentVersion < 8)
        {
            await ApplySchemaV8Async(connection, transaction, cancellationToken);
        }

        transaction.Commit();
    }

    public async Task SaveRunAsync(
        CollectionResult result,
        IReadOnlyList<Finding> findings,
        IReadOnlyList<MetricValue> metrics,
        IReadOnlyList<string> evaluatedRuleIds,
        IReadOnlyList<IndicatorResult> indicators,
        RunMetadata metadata,
        CancellationToken cancellationToken)
    {
        await MigrateAsync(cancellationToken);

        using var connection = await OpenConnectionAsync(cancellationToken);
        await ExecuteNonQueryAsync(connection, "PRAGMA foreign_keys = ON;", cancellationToken);
        using var transaction = connection.BeginTransaction();

        var now = DateTimeOffset.UtcNow.ToString("O");
        var domainName = result.DomainName ?? "(desconhecido)";

        await InsertRunAsync(connection, transaction, result, metadata, evaluatedRuleIds, cancellationToken);
        await InsertInventoryAsync(connection, transaction, result, cancellationToken);
        await InsertMetricsAsync(connection, transaction, result.RunId, metrics, cancellationToken);
        await InsertIndicatorResultsAsync(connection, transaction, result.RunId, indicators, now, cancellationToken);

        // Captura o estado anterior ANTES do upsert, para diferenciar mudancas.
        var priorState = await LoadCurrentStateAsync(connection, transaction, cancellationToken);

        // Historico por objeto: grava cada observacao deste run e atualiza o estado
        // atual (ultimo valor conhecido por objeto). Nada e apagado.
        await InsertObservationsAsync(connection, transaction, result, now, cancellationToken);

        // Timeline: materializa as mudancas observadas (created/deleted/membros/flags/...).
        await GenerateChangeEventsAsync(connection, transaction, result, priorState, now, cancellationToken);

        // Reconciliacao COM ESCOPO + JUSTIFICATIVA: so resolve achados cujas regras
        // realmente rodaram neste run (tipo de objeto coletado). O que ficou fora do
        // escopo e mantido (carried forward), nunca resolvido. Para cada achado que
        // a regra deixou de produzir, grava o porque: "Corrigido" (objeto ainda
        // existe), "Removido — na Lixeira do AD" ou "Removido — confirmado".
        if (evaluatedRuleIds.Count > 0)
        {
            await ReconcileResolvedAsync(
                connection, transaction, result, findings, evaluatedRuleIds, domainName, now, cancellationToken);
        }

        foreach (var finding in findings)
        {
            await UpsertFindingAsync(connection, transaction, finding, result.RunId, now, cancellationToken);
        }

        transaction.Commit();
    }

    public async Task<RunRecord?> GetLatestRunAsync(CancellationToken cancellationToken)
    {
        using var connection = await OpenConnectionAsync(cancellationToken);
        return await GetLatestRunAsync(connection, cancellationToken);
    }

    public async Task<DashboardData> GetDashboardAsync(CancellationToken cancellationToken)
    {
        using var connection = await OpenConnectionAsync(cancellationToken);

        var latest = await GetLatestRunAsync(connection, cancellationToken);
        var inventory = await ReadCurrentInventoryAsync(connection, latest?.RunId, cancellationToken);

        var activeWhere = $"""
            status IN ({InPlaceholders(ActiveStatuses.Length)})
            AND NOT EXISTS (
                SELECT 1 FROM risk_exceptions e
                WHERE e.stable_finding_key = findings.stable_finding_key AND e.expires_at > $now)
            """;
        var now = DateTimeOffset.UtcNow.ToString("O");

        var severity = new List<SeverityCount>();
        await using (var sevCmd = connection.CreateCommand())
        {
            sevCmd.CommandText = $"SELECT severity, COUNT(*) FROM findings WHERE {activeWhere} GROUP BY severity;";
            AddInParameters(sevCmd, ActiveStatuses);
            sevCmd.Parameters.AddWithValue("$now", now);
            await using var reader = await sevCmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                if (Enum.TryParse<Severity>(reader.GetString(0), out var sev))
                {
                    severity.Add(new SeverityCount(sev, reader.GetInt32(1)));
                }
            }
        }

        var categories = new List<CategoryCount>();
        await using (var catCmd = connection.CreateCommand())
        {
            catCmd.CommandText = $"SELECT category, COUNT(*) FROM findings WHERE {activeWhere} GROUP BY category;";
            AddInParameters(catCmd, ActiveStatuses);
            catCmd.Parameters.AddWithValue("$now", now);
            await using var reader = await catCmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                if (Enum.TryParse<FindingCategory>(reader.GetString(0), out var cat))
                {
                    categories.Add(new CategoryCount(cat, reader.GetInt32(1)));
                }
            }
        }

        var activeFindings = severity.Sum(s => s.Count);

        // Identity Score / Tier0 a partir das severidades x categorias dos achados ativos.
        var scoreInputs = new List<ScoreInput>();
        await using (var scoreCmd = connection.CreateCommand())
        {
            scoreCmd.CommandText = $"SELECT severity, category, COUNT(*) FROM findings WHERE {activeWhere} GROUP BY severity, category;";
            AddInParameters(scoreCmd, ActiveStatuses);
            scoreCmd.Parameters.AddWithValue("$now", now);
            await using var reader = await scoreCmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                if (Enum.TryParse<Severity>(reader.GetString(0), out var sev))
                {
                    var isTier0 = string.Equals(reader.GetString(1), nameof(FindingCategory.PrivilegedAccess), StringComparison.OrdinalIgnoreCase);
                    var count = reader.GetInt32(2);
                    for (var i = 0; i < count; i++)
                    {
                        scoreInputs.Add(new ScoreInput(sev, isTier0));
                    }
                }
            }
        }
        var score = IdentityScore.Compute(scoreInputs);

        async Task<int> CountActive(string sql)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            AddInParameters(cmd, ActiveStatuses);
            cmd.Parameters.AddWithValue("$now", now);
            var v = await cmd.ExecuteScalarAsync(cancellationToken);
            return v is null or DBNull ? 0 : Convert.ToInt32(v, CultureInfo.InvariantCulture);
        }

        var riskScore = await CountActive($"SELECT COALESCE(MAX(business_risk_score), 0) FROM findings WHERE {activeWhere};");
        var staleObjects = await CountActive($"SELECT COUNT(*) FROM findings WHERE {activeWhere} AND rule_id IN ('ADCLN-USER-STALE-001', 'ADCLN-COMP-STALE-003');");
        var privileged = await CountActive($"SELECT COUNT(*) FROM findings WHERE {activeWhere} AND rule_id = 'ADPRV-T0-GROUPS-001';");
        var metrics = new List<MetricValue>
        {
            new("riskScore", riskScore),
            new("findings", activeFindings),
            new("staleObjects", staleObjects),
            new("privilegedExposure", privileged)
        };

        return new DashboardData(latest, metrics, inventory, severity, categories, activeFindings,
            score.Score, score.Tier0Score, score.Health);
    }

    public async Task<IReadOnlyList<FindingRow>> GetFindingsAsync(
        string view,
        string? category,
        int limit,
        int offset,
        CancellationToken cancellationToken)
    {
        using var connection = await OpenConnectionAsync(cancellationToken);

        var hasException = """
            EXISTS (SELECT 1 FROM risk_exceptions e
                    WHERE e.stable_finding_key = f.stable_finding_key AND e.expires_at > $now)
            """;
        var statusClause = view?.ToLowerInvariant() switch
        {
            "exception" => $"{hasException}",
            "resolved" => $"f.status = 'Resolved' AND NOT {hasException}",
            _ => $"f.status IN ({InPlaceholders(ActiveStatuses.Length)}) AND NOT {hasException}"
        };
        var isActiveView = view?.ToLowerInvariant() is not ("exception" or "resolved");
        var categoryFilter = string.IsNullOrWhiteSpace(category) ? string.Empty : " AND f.category = $category";

        // limit <= 0 => sem teto: o painel de riscos exibe TODOS os achados.
        var paginate = limit > 0;
        var pageClause = paginate ? "LIMIT $limit OFFSET $offset" : string.Empty;

        var rows = new List<FindingRow>();
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT f.stable_finding_key, f.rule_id, f.title, f.category, f.severity, f.decision, f.status,
                   f.business_risk_score, f.object_display, f.first_seen, f.last_seen, f.evidence_json, f.last_run_id, f.object_key,
                   f.resolution_reason
            FROM findings f
            WHERE {statusClause} {categoryFilter}
            ORDER BY f.business_risk_score DESC, f.last_seen DESC
            {pageClause};
            """;
        if (isActiveView)
        {
            AddInParameters(command, ActiveStatuses);
        }
        command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToString("O"));
        if (paginate)
        {
            command.Parameters.AddWithValue("$limit", limit);
            command.Parameters.AddWithValue("$offset", Math.Max(0, offset));
        }
        if (categoryFilter.Length > 0)
        {
            command.Parameters.AddWithValue("$category", category!);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(ReadFindingRow(reader));
        }

        return rows;
    }

    public async Task<FindingRow?> GetFindingAsync(string stableFindingKey, CancellationToken cancellationToken)
    {
        using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT f.stable_finding_key, f.rule_id, f.title, f.category, f.severity, f.decision, f.status,
                   f.business_risk_score, f.object_display, f.first_seen, f.last_seen, f.evidence_json, f.last_run_id, f.object_key,
                   f.resolution_reason
            FROM findings f WHERE f.stable_finding_key = $key;
            """;
        command.Parameters.AddWithValue("$key", stableFindingKey);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadFindingRow(reader) : null;
    }

    private static FindingRow ReadFindingRow(SqliteDataReader reader) =>
        new(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            ParseEnum(reader.GetString(3), FindingCategory.Governance),
            ParseEnum(reader.GetString(4), Severity.Info),
            ParseEnum(reader.GetString(5), FindingDecision.Investigate),
            ParseEnum(reader.GetString(6), FindingStatus.Open),
            reader.GetInt32(7),
            reader.GetString(8),
            DateTimeOffset.Parse(reader.GetString(9), CultureInfo.InvariantCulture),
            DateTimeOffset.Parse(reader.GetString(10), CultureInfo.InvariantCulture),
            reader.IsDBNull(11) ? "{}" : reader.GetString(11),
            reader.IsDBNull(12) ? null : reader.GetString(12),
            reader.GetString(13),
            reader.IsDBNull(14) ? null : reader.GetString(14));

    public async Task<IReadOnlyList<InventoryState>> GetCurrentInventoryAsync(CancellationToken cancellationToken)
    {
        using var connection = await OpenConnectionAsync(cancellationToken);
        var latest = await GetLatestRunAsync(connection, cancellationToken);
        return await ReadCurrentInventoryAsync(connection, latest?.RunId, cancellationToken);
    }

    public async Task<IReadOnlyList<FocusObject>> GetObjectsForRefreshAsync(
        IReadOnlyCollection<string> objectKeys,
        CancellationToken cancellationToken)
    {
        var keys = objectKeys.Where(k => !string.IsNullOrWhiteSpace(k)).Distinct().ToList();
        if (keys.Count == 0)
        {
            return Array.Empty<FocusObject>();
        }

        using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        var ph = string.Join(", ", keys.Select((_, i) => $"$k{i}"));
        command.CommandText = $"""
            SELECT object_key, distinguished_name, object_type FROM current_object_state
            WHERE object_key IN ({ph});
            """;
        for (var i = 0; i < keys.Count; i++)
        {
            command.Parameters.AddWithValue($"$k{i}", keys[i]);
        }

        var result = new List<FocusObject>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var objectKey = reader.GetString(0);
            var dn = reader.GetString(1);
            var type = ParseEnum(reader.GetString(2), AdObjectType.User);
            if (!string.IsNullOrWhiteSpace(dn))
            {
                result.Add(new FocusObject(objectKey, dn, type));
            }
        }

        return result;
    }

    public async Task<IReadOnlyList<RunSummary>> GetRunsAsync(int limit, CancellationToken cancellationToken)
    {
        using var connection = await OpenConnectionAsync(cancellationToken);
        var runs = new List<RunSummary>();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT r.run_id, r.started_at, r.completed_at, r.coverage_mode, r.operator,
                   r.executed_as, r.credential_principal,
                   (SELECT COALESCE(SUM(total_count), 0) FROM run_inventory ri WHERE ri.run_id = r.run_id),
                   (SELECT COALESCE(metric_value, 0) FROM run_metrics rm WHERE rm.run_id = r.run_id AND rm.metric_key = 'findings')
            FROM runs r ORDER BY r.started_at DESC LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$limit", Math.Clamp(limit, 1, 200));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            runs.Add(new RunSummary(
                reader.GetString(0),
                DateTimeOffset.Parse(reader.GetString(1), CultureInfo.InvariantCulture),
                reader.IsDBNull(2) ? null : DateTimeOffset.Parse(reader.GetString(2), CultureInfo.InvariantCulture),
                reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.GetInt32(7),
                reader.GetInt32(8)));
        }

        return runs;
    }

    public async Task<IReadOnlyList<ObjectHistoryEntry>> GetObjectHistoryAsync(string objectKey, CancellationToken cancellationToken)
    {
        using var connection = await OpenConnectionAsync(cancellationToken);
        var entries = new List<ObjectHistoryEntry>();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT run_id, observed_at, attributes_json FROM object_observations
            WHERE object_key = $key ORDER BY observed_at DESC LIMIT 50;
            """;
        command.Parameters.AddWithValue("$key", objectKey);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            entries.Add(new ObjectHistoryEntry(
                reader.GetString(0),
                DateTimeOffset.Parse(reader.GetString(1), CultureInfo.InvariantCulture),
                reader.IsDBNull(2) ? "{}" : reader.GetString(2)));
        }

        return entries;
    }

    private static async Task<List<InventoryState>> ReadCurrentInventoryAsync(
        SqliteConnection connection,
        string? latestRunId,
        CancellationToken cancellationToken)
    {
        // Estado atual = ultimo valor conhecido por tipo, de QUALQUER run que tenha
        // coletado aquele tipo (Ready). Assim o inventario nunca zera por coleta
        // parcial e mostra os dados desde a ultima atualizacao de cada tipo.
        var inventory = new List<InventoryState>();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT object_type, total_count, started_at, run_id FROM (
                SELECT ri.object_type AS object_type, ri.total_count AS total_count,
                       r.started_at AS started_at, r.run_id AS run_id,
                       ROW_NUMBER() OVER (PARTITION BY ri.object_type ORDER BY r.started_at DESC) AS rn
                FROM run_inventory ri JOIN runs r ON r.run_id = ri.run_id
                WHERE ri.capability_state = 'Ready'
            ) WHERE rn = 1 ORDER BY object_type;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var runId = reader.GetString(3);
            inventory.Add(new InventoryState(
                reader.GetString(0),
                reader.GetInt32(1),
                DateTimeOffset.Parse(reader.GetString(2), CultureInfo.InvariantCulture),
                IsCurrent: latestRunId is not null && runId == latestRunId));
        }

        return inventory;
    }

    public async Task<RuleProfilesState> GetRuleProfilesAsync(CancellationToken cancellationToken)
    {
        using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT setting_value FROM app_settings WHERE setting_key = $key;";
        command.Parameters.AddWithValue("$key", RuleProfilesKey);
        var value = await command.ExecuteScalarAsync(cancellationToken) as string;

        RuleProfilesState? parsed = null;
        if (!string.IsNullOrWhiteSpace(value))
        {
            try { parsed = JsonSerializer.Deserialize<RuleProfilesState>(value); }
            catch (JsonException) { parsed = null; }
        }

        return BuiltInProfiles.Normalize(parsed);
    }

    public async Task SaveRuleProfilesAsync(RuleProfilesState state, CancellationToken cancellationToken)
    {
        var normalized = BuiltInProfiles.Normalize(state);
        using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO app_settings (setting_key, setting_value, updated_at)
            VALUES ($key, $value, $now)
            ON CONFLICT(setting_key) DO UPDATE SET setting_value = $value, updated_at = $now;
            """;
        command.Parameters.AddWithValue("$key", RuleProfilesKey);
        command.Parameters.AddWithValue("$value", JsonSerializer.Serialize(normalized));
        command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<RiskExceptionRecord> AddExceptionAsync(RiskExceptionInput input, CancellationToken cancellationToken)
    {
        var record = new RiskExceptionRecord(
            Guid.NewGuid().ToString("N"),
            input.StableFindingKey,
            input.Owner,
            input.Justification,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(Math.Clamp(input.ValidForDays, 1, 1825)));

        using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO risk_exceptions (exception_id, stable_finding_key, owner, justification, created_at, expires_at)
            VALUES ($id, $key, $owner, $justification, $created, $expires);
            """;
        command.Parameters.AddWithValue("$id", record.ExceptionId);
        command.Parameters.AddWithValue("$key", record.StableFindingKey);
        command.Parameters.AddWithValue("$owner", record.Owner);
        command.Parameters.AddWithValue("$justification", record.Justification);
        command.Parameters.AddWithValue("$created", record.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("$expires", record.ExpiresAt.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
        return record;
    }

    public async Task<IReadOnlyList<RiskExceptionRecord>> GetExceptionsAsync(bool includeExpired, CancellationToken cancellationToken)
    {
        using var connection = await OpenConnectionAsync(cancellationToken);
        var records = new List<RiskExceptionRecord>();
        await using var command = connection.CreateCommand();
        command.CommandText = includeExpired
            ? "SELECT exception_id, stable_finding_key, owner, justification, created_at, expires_at FROM risk_exceptions ORDER BY expires_at;"
            : "SELECT exception_id, stable_finding_key, owner, justification, created_at, expires_at FROM risk_exceptions WHERE expires_at > $now ORDER BY expires_at;";
        if (!includeExpired)
        {
            command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToString("O"));
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(new RiskExceptionRecord(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                DateTimeOffset.Parse(reader.GetString(4), CultureInfo.InvariantCulture),
                DateTimeOffset.Parse(reader.GetString(5), CultureInfo.InvariantCulture)));
        }

        return records;
    }

    public async Task<IReadOnlyList<RiskExceptionView>> GetExceptionViewsAsync(bool includeExpired, CancellationToken cancellationToken)
    {
        using var connection = await OpenConnectionAsync(cancellationToken);
        var views = new List<RiskExceptionView>();
        await using var command = connection.CreateCommand();
        var filter = includeExpired ? string.Empty : " WHERE e.expires_at > $now";
        command.CommandText = $"""
            SELECT e.exception_id, e.stable_finding_key, e.owner, e.justification, e.created_at, e.expires_at,
                   COALESCE(f.rule_id, ''), COALESCE(f.title, ''), COALESCE(f.category, 'Governance'),
                   COALESCE(f.severity, 'Info'), COALESCE(f.object_display, '')
            FROM risk_exceptions e
            LEFT JOIN findings f ON f.stable_finding_key = e.stable_finding_key
            {filter}
            ORDER BY e.expires_at;
            """;
        if (!includeExpired)
        {
            command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToString("O"));
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            views.Add(new RiskExceptionView(
                reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                DateTimeOffset.Parse(reader.GetString(4), CultureInfo.InvariantCulture),
                DateTimeOffset.Parse(reader.GetString(5), CultureInfo.InvariantCulture),
                reader.GetString(6), reader.GetString(7), reader.GetString(8), reader.GetString(9), reader.GetString(10)));
        }

        return views;
    }

    public async Task RemoveExceptionAsync(string exceptionId, CancellationToken cancellationToken)
    {
        using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM risk_exceptions WHERE exception_id = $id;";
        command.Parameters.AddWithValue("$id", exceptionId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<RunRecord?> GetLatestRunAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT run_id, collection_type, coverage_mode, started_at, completed_at, domain_dn, collector_version
            FROM runs ORDER BY started_at DESC LIMIT 1;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var domainDn = reader.IsDBNull(5) ? null : reader.GetString(5);
        return new RunRecord(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            DateTimeOffset.Parse(reader.GetString(3), CultureInfo.InvariantCulture),
            reader.IsDBNull(4) ? null : DateTimeOffset.Parse(reader.GetString(4), CultureInfo.InvariantCulture),
            domainDn,
            Core.Rules.HygieneRuleEngine.DeriveDomainName(domainDn),
            reader.GetString(6));
    }

    private static async Task InsertRunAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CollectionResult result,
        RunMetadata metadata,
        IReadOnlyList<string> evaluatedRuleIds,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO runs (run_id, collection_id, collection_type, coverage_mode, started_at,
                              completed_at, domain_dn, search_base_json, object_types_json,
                              feature_packs_json, collector_version, operator, executed_as,
                              credential_principal, evaluated_rules_json)
            VALUES ($run, $collection, $type, $coverage, $started, $completed, $domainDn,
                    $searchBase, $objectTypes, $featurePacks, $version, $operator, $executedAs,
                    $credential, $evaluated);
            """;
        command.Parameters.AddWithValue("$run", result.RunId);
        command.Parameters.AddWithValue("$collection", result.CollectionId);
        command.Parameters.AddWithValue("$type", result.CollectionType);
        command.Parameters.AddWithValue("$coverage", result.CoverageMode.ToString());
        command.Parameters.AddWithValue("$started", result.StartedAt.ToString("O"));
        command.Parameters.AddWithValue("$completed", result.CompletedAt.ToString("O"));
        command.Parameters.AddWithValue("$domainDn", (object?)result.DomainDn ?? DBNull.Value);
        command.Parameters.AddWithValue("$searchBase", JsonSerializer.Serialize(new[] { result.DomainDn }));
        command.Parameters.AddWithValue("$objectTypes", JsonSerializer.Serialize(result.Outcomes.Select(o => o.ObjectType.ToString())));
        command.Parameters.AddWithValue("$featurePacks", JsonSerializer.Serialize(result.FeaturePacks));
        command.Parameters.AddWithValue("$version", result.CollectorVersion);
        command.Parameters.AddWithValue("$operator", (object?)metadata.Operator ?? DBNull.Value);
        command.Parameters.AddWithValue("$executedAs", (object?)metadata.ExecutedAs ?? DBNull.Value);
        command.Parameters.AddWithValue("$credential", (object?)metadata.CredentialPrincipal ?? DBNull.Value);
        command.Parameters.AddWithValue("$evaluated", JsonSerializer.Serialize(evaluatedRuleIds));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertObservationsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CollectionResult result,
        string now,
        CancellationToken cancellationToken)
    {
        // Insere observacoes (historico) reaproveitando o mesmo comando.
        await using var obs = connection.CreateCommand();
        obs.Transaction = transaction;
        obs.CommandText = """
            INSERT INTO object_observations
                (observation_id, run_id, object_key, object_type, distinguished_name,
                 sam_account_name, display_name, attributes_json, observed_at, object_sid)
            VALUES ($id, $run, $key, $type, $dn, $sam, $display, $attrs, $now, $sid);
            """;
        var pId = obs.Parameters.Add("$id", SqliteType.Text);
        var pRun = obs.Parameters.Add("$run", SqliteType.Text);
        var pKey = obs.Parameters.Add("$key", SqliteType.Text);
        var pType = obs.Parameters.Add("$type", SqliteType.Text);
        var pDn = obs.Parameters.Add("$dn", SqliteType.Text);
        var pSam = obs.Parameters.Add("$sam", SqliteType.Text);
        var pDisplay = obs.Parameters.Add("$display", SqliteType.Text);
        var pAttrs = obs.Parameters.Add("$attrs", SqliteType.Text);
        var pSid = obs.Parameters.Add("$sid", SqliteType.Text);
        obs.Parameters.AddWithValue("$now", now);

        // Atualiza o estado atual (ultimo valor conhecido por objeto).
        await using var cur = connection.CreateCommand();
        cur.Transaction = transaction;
        cur.CommandText = """
            INSERT INTO current_object_state
                (object_key, object_type, distinguished_name, sam_account_name, display_name,
                 attributes_json, first_observed_at, last_observed_at, last_run_id, object_sid)
            VALUES ($key, $type, $dn, $sam, $display, $attrs, $now, $now, $run, $sid)
            ON CONFLICT(object_key) DO UPDATE SET
                object_type = $type, distinguished_name = $dn, sam_account_name = $sam,
                display_name = $display, attributes_json = $attrs, last_observed_at = $now, last_run_id = $run,
                object_sid = $sid;
            """;
        var cKey = cur.Parameters.Add("$key", SqliteType.Text);
        var cType = cur.Parameters.Add("$type", SqliteType.Text);
        var cDn = cur.Parameters.Add("$dn", SqliteType.Text);
        var cSam = cur.Parameters.Add("$sam", SqliteType.Text);
        var cDisplay = cur.Parameters.Add("$display", SqliteType.Text);
        var cAttrs = cur.Parameters.Add("$attrs", SqliteType.Text);
        var cSid = cur.Parameters.Add("$sid", SqliteType.Text);
        cur.Parameters.AddWithValue("$now", now);
        cur.Parameters.AddWithValue("$run", result.RunId);

        foreach (var obj in result.Objects)
        {
            var attrs = JsonSerializer.Serialize(obj.Attributes);
            var sid = (object?)obj.ObjectSid ?? DBNull.Value;
            pId.Value = Guid.NewGuid().ToString("N");
            pRun.Value = result.RunId;
            pKey.Value = obj.ObjectKey; pType.Value = obj.ObjectType.ToString();
            pDn.Value = obj.DistinguishedName; pSam.Value = (object?)obj.SamAccountName ?? DBNull.Value;
            pDisplay.Value = obj.DisplayName; pAttrs.Value = attrs; pSid.Value = sid;
            await obs.ExecuteNonQueryAsync(cancellationToken);

            cKey.Value = obj.ObjectKey; cType.Value = obj.ObjectType.ToString();
            cDn.Value = obj.DistinguishedName; cSam.Value = (object?)obj.SamAccountName ?? DBNull.Value;
            cDisplay.Value = obj.DisplayName; cAttrs.Value = attrs; cSid.Value = sid;
            await cur.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private sealed record PriorObject(string ObjectType, string Display, IReadOnlyDictionary<string, IReadOnlyList<string>> Attributes);

    private static async Task<Dictionary<string, PriorObject>> LoadCurrentStateAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<string, PriorObject>(StringComparer.OrdinalIgnoreCase);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT object_key, object_type, display_name, attributes_json FROM current_object_state;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            map[reader.GetString(0)] = new PriorObject(reader.GetString(1), reader.GetString(2), ParseAttributes(reader.GetString(3)));
        }
        return map;
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> ParseAttributes(string json)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json);
            if (parsed is null)
            {
                return new Dictionary<string, IReadOnlyList<string>>();
            }
            return parsed.ToDictionary(p => p.Key, p => (IReadOnlyList<string>)p.Value, StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, IReadOnlyList<string>>();
        }
    }

    private static async Task GenerateChangeEventsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CollectionResult result,
        IReadOnlyDictionary<string, PriorObject> priorState,
        string now,
        CancellationToken cancellationToken)
    {
        // Primeira coleta (sem estado anterior) = linha de base: nao gera "created" em massa.
        if (priorState.Count == 0)
        {
            return;
        }

        var observed = result.Objects.Select(o => o.ObjectKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var collectedTypes = result.Outcomes.Where(o => o.State == CapabilityState.Ready).Select(o => o.ObjectType).ToHashSet();
        if (result.Objects.Any(o => o.ObjectType == AdObjectType.Domain))
        {
            collectedTypes.Add(AdObjectType.Domain);
        }
        var scope = result.ScopeObjectKeys is null ? null : result.ScopeObjectKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var deletedGuids = result.DeletedObjectGuids.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var rows = new List<(string Key, string Type, string Display, ChangeType Change, string Attr, string? Old, string? New, Severity Sev)>();

        foreach (var obj in result.Objects)
        {
            if (!priorState.TryGetValue(obj.ObjectKey, out var prior))
            {
                rows.Add((obj.ObjectKey, obj.ObjectType.ToString(), obj.DisplayName, ChangeType.ObjectCreated,
                    "object", null, null, ChangeDetector.SeverityFor(ChangeType.ObjectCreated)));
                continue;
            }

            foreach (var c in ChangeDetector.Detect(obj, prior.Attributes))
            {
                rows.Add((obj.ObjectKey, obj.ObjectType.ToString(), obj.DisplayName, c.Type, c.Attribute, c.OldValue, c.NewValue, c.Severity));
            }
        }

        foreach (var (key, prior) in priorState)
        {
            if (observed.Contains(key) ||
                !Enum.TryParse<AdObjectType>(prior.ObjectType, out var type) ||
                !collectedTypes.Contains(type) ||
                (scope is not null && !scope.Contains(key)))
            {
                continue;
            }

            var guid = key.StartsWith("guid:", StringComparison.OrdinalIgnoreCase) ? key[5..] : null;
            var reason = guid is not null && deletedGuids.Contains(guid) ? "Removido - na Lixeira do AD" : "Removido - confirmado";
            rows.Add((key, prior.ObjectType, prior.Display, ChangeType.ObjectDeleted, "object", reason, null,
                ChangeDetector.SeverityFor(ChangeType.ObjectDeleted)));
        }

        if (rows.Count == 0)
        {
            return;
        }

        await using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = """
            INSERT INTO change_events
                (change_event_id, run_id, object_key, object_type, object_display, change_type,
                 attribute, old_value, new_value, severity, observed_at, details_json)
            VALUES ($id, $run, $key, $type, $display, $change, $attr, $old, $new, $sev, $now, '{}');
            """;
        var pId = insert.Parameters.Add("$id", SqliteType.Text);
        insert.Parameters.AddWithValue("$run", result.RunId);
        var pKey = insert.Parameters.Add("$key", SqliteType.Text);
        var pType = insert.Parameters.Add("$type", SqliteType.Text);
        var pDisplay = insert.Parameters.Add("$display", SqliteType.Text);
        var pChange = insert.Parameters.Add("$change", SqliteType.Text);
        var pAttr = insert.Parameters.Add("$attr", SqliteType.Text);
        var pOld = insert.Parameters.Add("$old", SqliteType.Text);
        var pNew = insert.Parameters.Add("$new", SqliteType.Text);
        var pSev = insert.Parameters.Add("$sev", SqliteType.Text);
        insert.Parameters.AddWithValue("$now", now);

        foreach (var r in rows)
        {
            pId.Value = Guid.NewGuid().ToString("N");
            pKey.Value = r.Key; pType.Value = r.Type; pDisplay.Value = r.Display;
            pChange.Value = r.Change.ToString(); pAttr.Value = r.Attr;
            pOld.Value = (object?)r.Old ?? DBNull.Value; pNew.Value = (object?)r.New ?? DBNull.Value;
            pSev.Value = r.Sev.ToString();
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task InsertInventoryAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CollectionResult result,
        CancellationToken cancellationToken)
    {
        foreach (var outcome in result.Outcomes)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO run_inventory (run_id, object_type, total_count, capability_state, message)
                VALUES ($run, $type, $count, $state, $message);
                """;
            command.Parameters.AddWithValue("$run", result.RunId);
            command.Parameters.AddWithValue("$type", outcome.ObjectType.ToString());
            command.Parameters.AddWithValue("$count", outcome.Count);
            command.Parameters.AddWithValue("$state", outcome.State.ToString());
            command.Parameters.AddWithValue("$message", (object?)outcome.Message ?? DBNull.Value);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task InsertMetricsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string runId,
        IReadOnlyList<MetricValue> metrics,
        CancellationToken cancellationToken)
    {
        foreach (var metric in metrics)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO run_metrics (run_id, metric_key, metric_value)
                VALUES ($run, $key, $value);
                """;
            command.Parameters.AddWithValue("$run", runId);
            command.Parameters.AddWithValue("$key", metric.Key);
            command.Parameters.AddWithValue("$value", metric.Value);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task InsertIndicatorResultsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string runId,
        IReadOnlyList<IndicatorResult> indicators,
        string now,
        CancellationToken cancellationToken)
    {
        foreach (var indicator in indicators)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT OR REPLACE INTO indicator_results
                    (run_id, indicator_id, title, category, is_custom, count, items_json, created_at)
                VALUES ($run, $id, $title, $category, $custom, $count, $items, $created);
                """;
            command.Parameters.AddWithValue("$run", runId);
            command.Parameters.AddWithValue("$id", indicator.Id);
            command.Parameters.AddWithValue("$title", indicator.Title);
            command.Parameters.AddWithValue("$category", indicator.Category);
            command.Parameters.AddWithValue("$custom", indicator.IsCustom ? 1 : 0);
            command.Parameters.AddWithValue("$count", indicator.Count);
            command.Parameters.AddWithValue("$items", JsonSerializer.Serialize(indicator.Items));
            command.Parameters.AddWithValue("$created", now);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<IndicatorResultRow>> GetLatestIndicatorsAsync(CancellationToken cancellationToken)
    {
        using var connection = await OpenConnectionAsync(cancellationToken);
        var latest = await GetLatestRunAsync(connection, cancellationToken);
        if (latest is null)
        {
            return [];
        }

        var rows = new List<IndicatorResultRow>();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT indicator_id, title, category, is_custom, count, items_json
            FROM indicator_results
            WHERE run_id = $run
            ORDER BY is_custom, category, count DESC;
            """;
        command.Parameters.AddWithValue("$run", latest.RunId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var itemsJson = reader.GetString(5);
            var items = JsonSerializer.Deserialize<List<IndicatorItem>>(itemsJson) ?? [];
            rows.Add(new IndicatorResultRow(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3) == 1,
                reader.GetInt32(4),
                items));
        }

        return rows;
    }

    public async Task AppendAuditAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        await MigrateAsync(cancellationToken);
        using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO audit_events
                (audit_event_id, timestamp, actor_user_id, actor_role, action, target_type,
                 target_id, source_ip, host_name, result, details_json)
            VALUES ($id, $ts, $actor, $role, $action, $ttype, $tid, $ip, $host, $result, $details);
            """;
        command.Parameters.AddWithValue("$id", Guid.NewGuid().ToString("N"));
        command.Parameters.AddWithValue("$ts", auditEvent.Timestamp.ToString("O"));
        command.Parameters.AddWithValue("$actor", auditEvent.ActorUserId);
        command.Parameters.AddWithValue("$role", auditEvent.ActorRole);
        command.Parameters.AddWithValue("$action", auditEvent.Action);
        command.Parameters.AddWithValue("$ttype", auditEvent.TargetType);
        command.Parameters.AddWithValue("$tid", auditEvent.TargetId);
        command.Parameters.AddWithValue("$ip", auditEvent.SourceIp);
        command.Parameters.AddWithValue("$host", auditEvent.HostName);
        command.Parameters.AddWithValue("$result", auditEvent.Result);
        command.Parameters.AddWithValue("$details", JsonSerializer.Serialize(auditEvent.Details));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditEvent>> GetAuditEventsAsync(int limit, CancellationToken cancellationToken)
    {
        using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT timestamp, actor_user_id, actor_role, action, target_type, target_id,
                   source_ip, host_name, result, details_json
            FROM audit_events ORDER BY timestamp DESC LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$limit", Math.Clamp(limit, 1, 1000));

        var events = new List<AuditEvent>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            IReadOnlyDictionary<string, string> details;
            try
            {
                details = JsonSerializer.Deserialize<Dictionary<string, string>>(reader.GetString(9))
                    ?? new Dictionary<string, string>();
            }
            catch (JsonException)
            {
                details = new Dictionary<string, string>();
            }

            events.Add(new AuditEvent(
                DateTimeOffset.Parse(reader.GetString(0), CultureInfo.InvariantCulture),
                reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4),
                reader.GetString(5), reader.GetString(6), reader.GetString(7), reader.GetString(8), details));
        }

        return events;
    }

    public async Task<int> GetUserCountAsync(CancellationToken cancellationToken)
    {
        await MigrateAsync(cancellationToken);
        using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM app_users;";
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? 0 : Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    public async Task CreateUserAsync(AppUserRecord user, CancellationToken cancellationToken)
    {
        using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO app_users (user_id, username, password_hash, salt, iterations, role, created_at)
            VALUES ($id, $u, $h, $s, $it, $r, $c);
            """;
        command.Parameters.AddWithValue("$id", user.UserId);
        command.Parameters.AddWithValue("$u", user.Username);
        command.Parameters.AddWithValue("$h", user.PasswordHash);
        command.Parameters.AddWithValue("$s", user.Salt);
        command.Parameters.AddWithValue("$it", user.Iterations);
        command.Parameters.AddWithValue("$r", user.Role);
        command.Parameters.AddWithValue("$c", user.CreatedAt.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<AppUserRecord?> GetUserByNameAsync(string username, CancellationToken cancellationToken)
    {
        using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT user_id, username, password_hash, salt, iterations, role, created_at FROM app_users WHERE username = $u;";
        command.Parameters.AddWithValue("$u", username);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }
        return new AppUserRecord(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
            reader.GetInt32(4), reader.GetString(5), DateTimeOffset.Parse(reader.GetString(6), CultureInfo.InvariantCulture));
    }

    public async Task<int> ResetAdminAsync(CancellationToken cancellationToken)
    {
        await MigrateAsync(cancellationToken);
        using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        // Conta antes de limpar, para reportar. Remove sessoes e usuarios.
        command.CommandText = """
            SELECT COUNT(*) FROM app_users;
            """;
        var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);

        await using var del = connection.CreateCommand();
        del.CommandText = "DELETE FROM app_sessions; DELETE FROM app_users;";
        await del.ExecuteNonQueryAsync(cancellationToken);
        return count;
    }

    public async Task CreateSessionAsync(AppSession session, CancellationToken cancellationToken)
    {
        using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO app_sessions (token, user_id, created_at, expires_at) VALUES ($t, $u, $c, $e);";
        command.Parameters.AddWithValue("$t", session.Token);
        command.Parameters.AddWithValue("$u", session.UserId);
        command.Parameters.AddWithValue("$c", session.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("$e", session.ExpiresAt.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<AppSession?> GetSessionAsync(string token, CancellationToken cancellationToken)
    {
        using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT token, user_id, created_at, expires_at FROM app_sessions WHERE token = $t;";
        command.Parameters.AddWithValue("$t", token);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }
        return new AppSession(reader.GetString(0), reader.GetString(1),
            DateTimeOffset.Parse(reader.GetString(2), CultureInfo.InvariantCulture),
            DateTimeOffset.Parse(reader.GetString(3), CultureInfo.InvariantCulture));
    }

    public async Task DeleteSessionAsync(string token, CancellationToken cancellationToken)
    {
        using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM app_sessions WHERE token = $t;";
        command.Parameters.AddWithValue("$t", token);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<ScheduleConfig> GetScheduleConfigAsync(CancellationToken cancellationToken)
    {
        await MigrateAsync(cancellationToken);
        using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT enabled, frequency, time_of_day, weekdays_json, interval_hours, host, protocol,
                   profile_name, last_run_at, next_run_at
            FROM schedule_config WHERE id = 1;
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return ScheduleConfig.Disabled();
        }

        var weekdays = new List<int>();
        try { weekdays = JsonSerializer.Deserialize<List<int>>(reader.GetString(3)) ?? new(); } catch (JsonException) { }
        Enum.TryParse<ScheduleFrequency>(reader.GetString(1), out var freq);

        return new ScheduleConfig(
            reader.GetInt32(0) != 0,
            freq,
            reader.GetString(2),
            weekdays,
            reader.GetInt32(4),
            reader.GetString(5),
            reader.GetString(6),
            reader.IsDBNull(7) ? null : reader.GetString(7),
            reader.IsDBNull(8) ? null : DateTimeOffset.Parse(reader.GetString(8), CultureInfo.InvariantCulture),
            reader.IsDBNull(9) ? null : DateTimeOffset.Parse(reader.GetString(9), CultureInfo.InvariantCulture));
    }

    public async Task SaveScheduleConfigAsync(ScheduleConfig config, CancellationToken cancellationToken)
    {
        using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO schedule_config (id, enabled, frequency, time_of_day, weekdays_json, interval_hours,
                                         host, protocol, profile_name, last_run_at, next_run_at)
            VALUES (1, $enabled, $freq, $time, $days, $interval, $host, $proto, $profile, $last, $next)
            ON CONFLICT(id) DO UPDATE SET
                enabled = $enabled, frequency = $freq, time_of_day = $time, weekdays_json = $days,
                interval_hours = $interval, host = $host, protocol = $proto, profile_name = $profile,
                last_run_at = $last, next_run_at = $next;
            """;
        command.Parameters.AddWithValue("$enabled", config.Enabled ? 1 : 0);
        command.Parameters.AddWithValue("$freq", config.Frequency.ToString());
        command.Parameters.AddWithValue("$time", config.TimeOfDay);
        command.Parameters.AddWithValue("$days", JsonSerializer.Serialize(config.Weekdays));
        command.Parameters.AddWithValue("$interval", config.IntervalHours);
        command.Parameters.AddWithValue("$host", config.Host);
        command.Parameters.AddWithValue("$proto", config.Protocol);
        command.Parameters.AddWithValue("$profile", (object?)config.ProfileName ?? DBNull.Value);
        command.Parameters.AddWithValue("$last", (object?)config.LastRunAt?.ToString("O") ?? DBNull.Value);
        command.Parameters.AddWithValue("$next", (object?)config.NextRunAt?.ToString("O") ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ChangeEventRow>> GetChangesAsync(int limit, int? sinceHours, string? changeType, CancellationToken cancellationToken)
    {
        using var connection = await OpenConnectionAsync(cancellationToken);
        var filters = new List<string>();
        if (sinceHours is > 0) filters.Add("observed_at >= $since");
        if (!string.IsNullOrWhiteSpace(changeType)) filters.Add("change_type = $type");
        var where = filters.Count > 0 ? "WHERE " + string.Join(" AND ", filters) : string.Empty;

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT run_id, observed_at, object_key, object_type, object_display, change_type,
                   attribute, old_value, new_value, severity
            FROM change_events {where} ORDER BY observed_at DESC LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$limit", Math.Clamp(limit, 1, 2000));
        if (sinceHours is > 0) command.Parameters.AddWithValue("$since", DateTimeOffset.UtcNow.AddHours(-sinceHours.Value).ToString("O"));
        if (!string.IsNullOrWhiteSpace(changeType)) command.Parameters.AddWithValue("$type", changeType);

        var rows = new List<ChangeEventRow>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(ReadChangeRow(reader));
        }
        return rows;
    }

    public async Task<IReadOnlyList<ChangeCount>> GetChangeSummaryAsync(int sinceHours, CancellationToken cancellationToken)
    {
        using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT change_type, COUNT(*) FROM change_events WHERE observed_at >= $since GROUP BY change_type;";
        command.Parameters.AddWithValue("$since", DateTimeOffset.UtcNow.AddHours(-Math.Max(1, sinceHours)).ToString("O"));

        var rows = new List<ChangeCount>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new ChangeCount(reader.GetString(0), reader.GetInt32(1)));
        }
        return rows;
    }

    public async Task<int> CountNewFindingsAsync(int sinceHours, CancellationToken cancellationToken)
    {
        using var connection = await OpenConnectionAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow.ToString("O");
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT COUNT(*) FROM findings f
            WHERE f.first_seen >= $since
              AND f.status IN ({InPlaceholders(ActiveStatuses.Length)})
              AND NOT EXISTS (SELECT 1 FROM risk_exceptions e
                              WHERE e.stable_finding_key = f.stable_finding_key AND e.expires_at > $now);
            """;
        AddInParameters(command, ActiveStatuses);
        command.Parameters.AddWithValue("$since", DateTimeOffset.UtcNow.AddHours(-Math.Max(1, sinceHours)).ToString("O"));
        command.Parameters.AddWithValue("$now", now);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? 0 : Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    public async Task<IReadOnlyList<SearchHit>> SearchObjectsAsync(string query, int limit, CancellationToken cancellationToken)
    {
        var trimmed = query?.Trim() ?? string.Empty;
        if (trimmed.Length < 2)
        {
            return Array.Empty<SearchHit>();
        }

        using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT object_key, object_type, display_name, distinguished_name, object_sid
            FROM current_object_state
            WHERE sam_account_name LIKE $q OR display_name LIKE $q OR distinguished_name LIKE $q
               OR object_key LIKE $q OR object_sid LIKE $q OR attributes_json LIKE $q
            ORDER BY display_name LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$q", "%" + trimmed + "%");
        command.Parameters.AddWithValue("$limit", Math.Clamp(limit, 1, 100));

        var hits = new List<SearchHit>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            hits.Add(new SearchHit(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3)));
        }
        return hits;
    }

    public async Task<ObjectDetail?> GetObjectDetailAsync(string objectKey, CancellationToken cancellationToken)
    {
        using var connection = await OpenConnectionAsync(cancellationToken);

        string type, dn, display, attrsJson; string? sid; DateTimeOffset? lastObserved;
        await using (var head = connection.CreateCommand())
        {
            head.CommandText = """
                SELECT object_type, distinguished_name, display_name, attributes_json, object_sid, last_observed_at
                FROM current_object_state WHERE object_key = $key;
                """;
            head.Parameters.AddWithValue("$key", objectKey);
            await using var reader = await head.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }
            type = reader.GetString(0); dn = reader.GetString(1); display = reader.GetString(2);
            attrsJson = reader.GetString(3); sid = reader.IsDBNull(4) ? null : reader.GetString(4);
            lastObserved = reader.IsDBNull(5) ? null : DateTimeOffset.Parse(reader.GetString(5), CultureInfo.InvariantCulture);
        }

        var attrs = ParseAttributes(attrsJson);
        var fields = BuildObjectFields(type, dn, sid, attrs);

        var findings = new List<FindingRow>();
        await using (var fcmd = connection.CreateCommand())
        {
            fcmd.CommandText = """
                SELECT f.stable_finding_key, f.rule_id, f.title, f.category, f.severity, f.decision, f.status,
                       f.business_risk_score, f.object_display, f.first_seen, f.last_seen, f.evidence_json, f.last_run_id, f.object_key,
                       f.resolution_reason
                FROM findings f WHERE f.object_key = $key ORDER BY f.business_risk_score DESC;
                """;
            fcmd.Parameters.AddWithValue("$key", objectKey);
            await using var reader = await fcmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                findings.Add(ReadFindingRow(reader));
            }
        }

        var changes = new List<ChangeEventRow>();
        await using (var ccmd = connection.CreateCommand())
        {
            ccmd.CommandText = """
                SELECT run_id, observed_at, object_key, object_type, object_display, change_type,
                       attribute, old_value, new_value, severity
                FROM change_events WHERE object_key = $key ORDER BY observed_at DESC LIMIT 100;
                """;
            ccmd.Parameters.AddWithValue("$key", objectKey);
            await using var reader = await ccmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                changes.Add(ReadChangeRow(reader));
            }
        }

        return new ObjectDetail(objectKey, type, display, dn, lastObserved, fields, findings, changes);
    }

    private static ChangeEventRow ReadChangeRow(SqliteDataReader reader) =>
        new(
            reader.GetString(0),
            DateTimeOffset.Parse(reader.GetString(1), CultureInfo.InvariantCulture),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetString(6),
            reader.IsDBNull(7) ? null : reader.GetString(7),
            reader.IsDBNull(8) ? null : reader.GetString(8),
            reader.GetString(9));

    private static IReadOnlyList<ObjectField> BuildObjectFields(
        string type, string dn, string? sid, IReadOnlyDictionary<string, IReadOnlyList<string>> attrs)
    {
        var fields = new List<ObjectField>();
        void Add(string label, string? value) { if (!string.IsNullOrWhiteSpace(value)) fields.Add(new ObjectField(label, value!)); }
        string? V(string k) => attrs.TryGetValue(k, out var v) && v.Count > 0 ? v[0] : null;
        int Count(string k) => attrs.TryGetValue(k, out var v) ? v.Count : 0;

        Add("displayName", V("displayName"));
        Add("sam", V("sAMAccountName"));
        Add("objectSid", sid);
        Add("distinguishedName", dn);

        if (type == nameof(AdObjectType.User) || type == nameof(AdObjectType.Computer))
        {
            var uac = V("userAccountControl");
            var enabled = AdAttributes.IsEnabled(uac);
            Add("enabled", enabled is null ? null : (enabled.Value ? "true" : "false"));
            Add("lastLogon", AdAttributes.ParseFileTime(V("lastLogonTimestamp"))?.ToString("u"));
            Add("pwdLastSet", AdAttributes.ParseFileTime(V("pwdLastSet"))?.ToString("u"));
            Add("userPrincipalName", V("userPrincipalName"));
            Add("manager", V("manager"));
            Add("operatingSystem", V("operatingSystem"));
            if (Count("servicePrincipalName") > 0) Add("servicePrincipalName", string.Join(", ", attrs["servicePrincipalName"]));
            if (Count("memberOf") > 0) Add("memberOf", Count("memberOf").ToString());
        }
        else if (type == nameof(AdObjectType.Group))
        {
            Add("description", V("description"));
            Add("managedBy", V("managedBy"));
            Add("memberCount", Count("member").ToString());
        }
        else if (type == nameof(AdObjectType.GroupPolicyContainer))
        {
            Add("versionNumber", V("versionNumber"));
            Add("flags", V("flags"));
            Add("gPLink", V("gPLink"));
        }

        return fields;
    }

    private static async Task ReconcileResolvedAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CollectionResult result,
        IReadOnlyList<Finding> findings,
        IReadOnlyList<string> evaluatedRuleIds,
        string domainName,
        string now,
        CancellationToken cancellationToken)
    {
        var producedKeys = findings.Select(f => f.StableFindingKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var observedKeys = result.Objects.Select(o => o.ObjectKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var deletedGuids = result.DeletedObjectGuids.ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Reavaliacao dirigida: so os objetos em foco entram na reconciliacao. Num run
        // normal (scope null), vale todo o tipo coletado.
        var scope = result.ScopeObjectKeys is null
            ? null
            : result.ScopeObjectKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Achados ativos das regras que rodaram neste run.
        var candidates = new List<(string Key, string ObjectKey)>();
        await using (var select = connection.CreateCommand())
        {
            select.Transaction = transaction;
            var ph = string.Join(", ", evaluatedRuleIds.Select((_, i) => $"$r{i}"));
            select.CommandText = $"""
                SELECT stable_finding_key, object_key FROM findings
                WHERE domain_name = $domain AND status IN ('New', 'Open', 'Recurring')
                  AND rule_id IN ({ph});
                """;
            select.Parameters.AddWithValue("$domain", domainName);
            for (var i = 0; i < evaluatedRuleIds.Count; i++)
            {
                select.Parameters.AddWithValue($"$r{i}", evaluatedRuleIds[i]);
            }

            await using var reader = await select.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                candidates.Add((reader.GetString(0), reader.GetString(1)));
            }
        }

        await using var update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText = """
            UPDATE findings SET status = 'Resolved', resolution_reason = $reason, updated_at = $now
            WHERE stable_finding_key = $key;
            """;
        var pReason = update.Parameters.Add("$reason", SqliteType.Text);
        update.Parameters.AddWithValue("$now", now);
        var pKey = update.Parameters.Add("$key", SqliteType.Text);

        foreach (var (key, objectKey) in candidates)
        {
            // Fora do escopo dirigido -> nao reavaliado, mantem (carry forward).
            if (scope is not null && !scope.Contains(objectKey))
            {
                continue;
            }

            // Ainda produzido neste run -> permanece ativo (reativado no upsert).
            if (producedKeys.Contains(key))
            {
                continue;
            }

            // Codigo estavel (a UI localiza): Fixed = objeto ainda existe e a condicao
            // sumiu; RemovedInRecycleBin / RemovedConfirmed = objeto desapareceu.
            string reason;
            if (observedKeys.Contains(objectKey))
            {
                reason = "Fixed";
            }
            else
            {
                var guid = objectKey.StartsWith("guid:", StringComparison.OrdinalIgnoreCase)
                    ? objectKey[5..]
                    : null;
                reason = guid is not null && deletedGuids.Contains(guid)
                    ? "RemovedInRecycleBin"
                    : "RemovedConfirmed";
            }

            pReason.Value = reason;
            pKey.Value = key;
            await update.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task UpsertFindingAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Finding finding,
        string runId,
        string now,
        CancellationToken cancellationToken)
    {
        var evidenceJson = JsonSerializer.Serialize(finding.Evidence);

        await using var update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText = """
            UPDATE findings SET
                last_seen = $now,
                last_run_id = $run,
                seen_count = seen_count + 1,
                rule_id = $rule,
                title = $title,
                category = $category,
                severity = $severity,
                decision = $decision,
                business_risk_score = $score,
                object_display = $display,
                evidence_json = $evidence,
                updated_at = $now,
                resolution_reason = NULL,
                status = CASE
                    WHEN status = 'AcceptedRisk' THEN 'AcceptedRisk'
                    WHEN seen_count + 1 >= 3 THEN 'Recurring'
                    ELSE 'Open'
                END
            WHERE stable_finding_key = $key;
            """;
        update.Parameters.AddWithValue("$now", now);
        update.Parameters.AddWithValue("$run", runId);
        update.Parameters.AddWithValue("$rule", finding.RuleId);
        update.Parameters.AddWithValue("$title", finding.Title);
        update.Parameters.AddWithValue("$category", finding.Category.ToString());
        update.Parameters.AddWithValue("$severity", finding.Severity.ToString());
        update.Parameters.AddWithValue("$decision", finding.Decision.ToString());
        update.Parameters.AddWithValue("$score", finding.BusinessRiskScore);
        update.Parameters.AddWithValue("$display", finding.ObjectDisplay);
        update.Parameters.AddWithValue("$evidence", evidenceJson);
        update.Parameters.AddWithValue("$key", finding.StableFindingKey);

        var affected = await update.ExecuteNonQueryAsync(cancellationToken);
        if (affected > 0)
        {
            return;
        }

        await using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = """
            INSERT INTO findings (finding_id, stable_finding_key, rule_id, title, category, severity, status,
                                  decision, business_risk_score, domain_name, object_key, object_display,
                                  first_seen, last_seen, last_run_id, seen_count, evidence_json,
                                  created_at, updated_at)
            VALUES ($id, $key, $rule, $title, $category, $severity, 'New', $decision, $score, $domain, $objectKey,
                    $display, $now, $now, $run, 1, $evidence, $now, $now);
            """;
        insert.Parameters.AddWithValue("$id", Guid.NewGuid().ToString("N"));
        insert.Parameters.AddWithValue("$key", finding.StableFindingKey);
        insert.Parameters.AddWithValue("$rule", finding.RuleId);
        insert.Parameters.AddWithValue("$title", finding.Title);
        insert.Parameters.AddWithValue("$category", finding.Category.ToString());
        insert.Parameters.AddWithValue("$severity", finding.Severity.ToString());
        insert.Parameters.AddWithValue("$decision", finding.Decision.ToString());
        insert.Parameters.AddWithValue("$score", finding.BusinessRiskScore);
        insert.Parameters.AddWithValue("$domain", finding.DomainName);
        insert.Parameters.AddWithValue("$objectKey", finding.ObjectKey);
        insert.Parameters.AddWithValue("$display", finding.ObjectDisplay);
        insert.Parameters.AddWithValue("$now", now);
        insert.Parameters.AddWithValue("$run", runId);
        insert.Parameters.AddWithValue("$evidence", evidenceJson);
        await insert.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        using var key = await keyStore.GetOrCreateAsync(cancellationToken);
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = options.DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Default,
            Password = Convert.ToHexString(key.Key)
        };

        var connection = new SqliteConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static async Task ApplySchemaV1Async(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        foreach (var statement in SchemaV1Statements())
        {
            await ExecuteNonQueryAsync(connection, statement, cancellationToken, transaction);
        }

        foreach (var role in Enum.GetNames<AppRole>())
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT OR IGNORE INTO local_roles(role_name) VALUES ($role_name);";
            command.Parameters.AddWithValue("$role_name", role);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var migration = connection.CreateCommand();
        migration.Transaction = transaction;
        migration.CommandText = """
            INSERT OR IGNORE INTO schema_migrations(version, name, applied_at, checksum)
            VALUES (1, 'initial_product_schema', $applied_at, 'schema-v1');
            """;
        migration.Parameters.AddWithValue("$applied_at", DateTimeOffset.UtcNow.ToString("O"));
        await migration.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ApplySchemaV2Async(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        foreach (var statement in SchemaV2Statements())
        {
            await ExecuteNonQueryAsync(connection, statement, cancellationToken, transaction);
        }

        await using var migration = connection.CreateCommand();
        migration.Transaction = transaction;
        migration.CommandText = """
            INSERT OR IGNORE INTO schema_migrations(version, name, applied_at, checksum)
            VALUES (2, 'collection_results', $applied_at, 'schema-v2');
            """;
        migration.Parameters.AddWithValue("$applied_at", DateTimeOffset.UtcNow.ToString("O"));
        await migration.ExecuteNonQueryAsync(cancellationToken);
    }

    private static IReadOnlyList<string> SchemaV1Statements()
    {
        return
        [
            """
            CREATE TABLE IF NOT EXISTS schema_migrations (
                version INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                applied_at TEXT NOT NULL,
                checksum TEXT NOT NULL
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS app_settings (
                setting_key TEXT PRIMARY KEY,
                setting_value TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS local_users (
                user_id TEXT PRIMARY KEY,
                username TEXT NOT NULL UNIQUE,
                display_name TEXT NOT NULL,
                password_hash TEXT NOT NULL,
                password_salt TEXT NOT NULL,
                password_algorithm TEXT NOT NULL,
                disabled INTEGER NOT NULL DEFAULT 0,
                failed_attempts INTEGER NOT NULL DEFAULT 0,
                locked_until TEXT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS local_roles (
                role_name TEXT PRIMARY KEY
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS local_user_roles (
                user_id TEXT NOT NULL,
                role_name TEXT NOT NULL,
                PRIMARY KEY (user_id, role_name),
                FOREIGN KEY (user_id) REFERENCES local_users(user_id) ON DELETE CASCADE,
                FOREIGN KEY (role_name) REFERENCES local_roles(role_name) ON DELETE RESTRICT
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS audit_events (
                audit_event_id TEXT PRIMARY KEY,
                timestamp TEXT NOT NULL,
                actor_user_id TEXT NOT NULL,
                actor_role TEXT NOT NULL,
                action TEXT NOT NULL,
                target_type TEXT NOT NULL,
                target_id TEXT NOT NULL,
                source_ip TEXT NOT NULL,
                host_name TEXT NOT NULL,
                result TEXT NOT NULL,
                details_json TEXT NOT NULL
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS collection_jobs (
                collection_job_id TEXT PRIMARY KEY,
                status TEXT NOT NULL,
                requested_by_user_id TEXT NOT NULL,
                requested_at TEXT NOT NULL,
                started_at TEXT NULL,
                completed_at TEXT NULL,
                target_host TEXT NOT NULL,
                protocol TEXT NOT NULL,
                scope_json TEXT NOT NULL
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS runs (
                run_id TEXT PRIMARY KEY,
                collection_id TEXT NOT NULL,
                collection_type TEXT NOT NULL,
                coverage_mode TEXT NOT NULL,
                started_at TEXT NOT NULL,
                completed_at TEXT NULL,
                domain_dn TEXT NULL,
                search_base_json TEXT NOT NULL,
                object_types_json TEXT NOT NULL,
                feature_packs_json TEXT NOT NULL,
                collector_version TEXT NOT NULL
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS findings (
                finding_id TEXT PRIMARY KEY,
                stable_finding_key TEXT NOT NULL UNIQUE,
                rule_id TEXT NOT NULL,
                title TEXT NOT NULL,
                severity TEXT NOT NULL,
                status TEXT NOT NULL,
                business_risk_score INTEGER NOT NULL,
                domain_name TEXT NOT NULL,
                object_key TEXT NOT NULL,
                first_seen TEXT NOT NULL,
                last_seen TEXT NOT NULL,
                evidence_json TEXT NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );
            """,
            "CREATE INDEX IF NOT EXISTS ix_audit_events_timestamp ON audit_events(timestamp);",
            "CREATE INDEX IF NOT EXISTS ix_collection_jobs_status ON collection_jobs(status);",
            "CREATE INDEX IF NOT EXISTS ix_runs_started_at ON runs(started_at);",
            "CREATE INDEX IF NOT EXISTS ix_runs_collection_id ON runs(collection_id);",
            "CREATE INDEX IF NOT EXISTS ix_findings_rule_id ON findings(rule_id);",
            "CREATE INDEX IF NOT EXISTS ix_findings_status ON findings(status);",
            "CREATE INDEX IF NOT EXISTS ix_findings_severity ON findings(severity);",
            "CREATE INDEX IF NOT EXISTS ix_findings_business_risk_score ON findings(business_risk_score);"
        ];
    }

    private static async Task ApplySchemaV3Async(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        foreach (var statement in SchemaV3Statements())
        {
            await ExecuteNonQueryAsync(connection, statement, cancellationToken, transaction);
        }

        await using var migration = connection.CreateCommand();
        migration.Transaction = transaction;
        migration.CommandText = """
            INSERT OR IGNORE INTO schema_migrations(version, name, applied_at, checksum)
            VALUES (3, 'categories_and_exceptions', $applied_at, 'schema-v3');
            """;
        migration.Parameters.AddWithValue("$applied_at", DateTimeOffset.UtcNow.ToString("O"));
        await migration.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ApplySchemaV4Async(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        foreach (var statement in SchemaV4Statements())
        {
            await ExecuteNonQueryAsync(connection, statement, cancellationToken, transaction);
        }

        await using var migration = connection.CreateCommand();
        migration.Transaction = transaction;
        migration.CommandText = """
            INSERT OR IGNORE INTO schema_migrations(version, name, applied_at, checksum)
            VALUES (4, 'object_history_and_run_metadata', $applied_at, 'schema-v4');
            """;
        migration.Parameters.AddWithValue("$applied_at", DateTimeOffset.UtcNow.ToString("O"));
        await migration.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ApplySchemaV5Async(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        // Justificativa da resolucao de um achado (Corrigido / Removido — na Lixeira /
        // Removido — confirmado).
        await ExecuteNonQueryAsync(
            connection,
            "ALTER TABLE findings ADD COLUMN resolution_reason TEXT NULL;",
            cancellationToken,
            transaction);

        await using var migration = connection.CreateCommand();
        migration.Transaction = transaction;
        migration.CommandText = """
            INSERT OR IGNORE INTO schema_migrations(version, name, applied_at, checksum)
            VALUES (5, 'finding_resolution_reason', $applied_at, 'schema-v5');
            """;
        migration.Parameters.AddWithValue("$applied_at", DateTimeOffset.UtcNow.ToString("O"));
        await migration.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ApplySchemaV6Async(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        await ExecuteNonQueryAsync(
            connection,
            """
            CREATE TABLE IF NOT EXISTS change_events (
                change_event_id TEXT PRIMARY KEY,
                run_id TEXT NOT NULL,
                object_key TEXT NOT NULL,
                object_type TEXT NOT NULL,
                object_display TEXT NOT NULL,
                change_type TEXT NOT NULL,
                attribute TEXT NOT NULL,
                old_value TEXT NULL,
                new_value TEXT NULL,
                severity TEXT NOT NULL,
                observed_at TEXT NOT NULL,
                details_json TEXT NOT NULL
            );
            """,
            cancellationToken,
            transaction);
        await ExecuteNonQueryAsync(connection, "CREATE INDEX IF NOT EXISTS ix_change_events_observed_at ON change_events(observed_at);", cancellationToken, transaction);
        await ExecuteNonQueryAsync(connection, "CREATE INDEX IF NOT EXISTS ix_change_events_object ON change_events(object_key);", cancellationToken, transaction);

        // SID por objeto para busca global e exibicao na pagina do objeto.
        await ExecuteNonQueryAsync(connection, "ALTER TABLE current_object_state ADD COLUMN object_sid TEXT NULL;", cancellationToken, transaction);
        await ExecuteNonQueryAsync(connection, "ALTER TABLE object_observations ADD COLUMN object_sid TEXT NULL;", cancellationToken, transaction);

        await using var migration = connection.CreateCommand();
        migration.Transaction = transaction;
        migration.CommandText = """
            INSERT OR IGNORE INTO schema_migrations(version, name, applied_at, checksum)
            VALUES (6, 'change_events', $applied_at, 'schema-v6');
            """;
        migration.Parameters.AddWithValue("$applied_at", DateTimeOffset.UtcNow.ToString("O"));
        await migration.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ApplySchemaV7Async(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        await ExecuteNonQueryAsync(connection,
            """
            CREATE TABLE IF NOT EXISTS app_users (
                user_id TEXT PRIMARY KEY,
                username TEXT NOT NULL UNIQUE,
                password_hash TEXT NOT NULL,
                salt TEXT NOT NULL,
                iterations INTEGER NOT NULL,
                role TEXT NOT NULL,
                created_at TEXT NOT NULL
            );
            """, cancellationToken, transaction);
        await ExecuteNonQueryAsync(connection,
            """
            CREATE TABLE IF NOT EXISTS app_sessions (
                token TEXT PRIMARY KEY,
                user_id TEXT NOT NULL,
                created_at TEXT NOT NULL,
                expires_at TEXT NOT NULL
            );
            """, cancellationToken, transaction);
        await ExecuteNonQueryAsync(connection,
            """
            CREATE TABLE IF NOT EXISTS schedule_config (
                id INTEGER PRIMARY KEY CHECK (id = 1),
                enabled INTEGER NOT NULL,
                frequency TEXT NOT NULL,
                time_of_day TEXT NOT NULL,
                weekdays_json TEXT NOT NULL,
                interval_hours INTEGER NOT NULL,
                host TEXT NOT NULL,
                protocol TEXT NOT NULL,
                profile_name TEXT NULL,
                last_run_at TEXT NULL,
                next_run_at TEXT NULL
            );
            """, cancellationToken, transaction);

        await using var migration = connection.CreateCommand();
        migration.Transaction = transaction;
        migration.CommandText = """
            INSERT OR IGNORE INTO schema_migrations(version, name, applied_at, checksum)
            VALUES (7, 'auth_and_schedule', $applied_at, 'schema-v7');
            """;
        migration.Parameters.AddWithValue("$applied_at", DateTimeOffset.UtcNow.ToString("O"));
        await migration.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ApplySchemaV8Async(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        await ExecuteNonQueryAsync(connection,
            """
            CREATE TABLE IF NOT EXISTS indicator_results (
                run_id TEXT NOT NULL,
                indicator_id TEXT NOT NULL,
                title TEXT NOT NULL,
                category TEXT NOT NULL,
                is_custom INTEGER NOT NULL,
                count INTEGER NOT NULL,
                items_json TEXT NOT NULL,
                created_at TEXT NOT NULL,
                PRIMARY KEY (run_id, indicator_id),
                FOREIGN KEY (run_id) REFERENCES runs(run_id) ON DELETE CASCADE
            );
            """, cancellationToken, transaction);

        await using var migration = connection.CreateCommand();
        migration.Transaction = transaction;
        migration.CommandText = """
            INSERT OR IGNORE INTO schema_migrations(version, name, applied_at, checksum)
            VALUES (8, 'indicator_results', $applied_at, 'schema-v8');
            """;
        migration.Parameters.AddWithValue("$applied_at", DateTimeOffset.UtcNow.ToString("O"));
        await migration.ExecuteNonQueryAsync(cancellationToken);
    }

    private static IReadOnlyList<string> SchemaV4Statements()
    {
        return
        [
            "ALTER TABLE runs ADD COLUMN operator TEXT NULL;",
            "ALTER TABLE runs ADD COLUMN executed_as TEXT NULL;",
            "ALTER TABLE runs ADD COLUMN credential_principal TEXT NULL;",
            "ALTER TABLE runs ADD COLUMN evaluated_rules_json TEXT NOT NULL DEFAULT '[]';",
            """
            CREATE TABLE IF NOT EXISTS object_observations (
                observation_id TEXT PRIMARY KEY,
                run_id TEXT NOT NULL,
                object_key TEXT NOT NULL,
                object_type TEXT NOT NULL,
                distinguished_name TEXT NOT NULL,
                sam_account_name TEXT NULL,
                display_name TEXT NOT NULL,
                attributes_json TEXT NOT NULL,
                observed_at TEXT NOT NULL,
                FOREIGN KEY (run_id) REFERENCES runs(run_id) ON DELETE CASCADE
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS current_object_state (
                object_key TEXT PRIMARY KEY,
                object_type TEXT NOT NULL,
                distinguished_name TEXT NOT NULL,
                sam_account_name TEXT NULL,
                display_name TEXT NOT NULL,
                attributes_json TEXT NOT NULL,
                first_observed_at TEXT NOT NULL,
                last_observed_at TEXT NOT NULL,
                last_run_id TEXT NOT NULL
            );
            """,
            "CREATE INDEX IF NOT EXISTS ix_object_observations_key ON object_observations(object_key);",
            "CREATE INDEX IF NOT EXISTS ix_object_observations_run ON object_observations(run_id);",
            "CREATE INDEX IF NOT EXISTS ix_current_object_state_type ON current_object_state(object_type);"
        ];
    }

    private static IReadOnlyList<string> SchemaV3Statements()
    {
        return
        [
            "ALTER TABLE findings ADD COLUMN category TEXT NOT NULL DEFAULT 'Governance';",
            """
            CREATE TABLE IF NOT EXISTS risk_exceptions (
                exception_id TEXT PRIMARY KEY,
                stable_finding_key TEXT NOT NULL,
                owner TEXT NOT NULL,
                justification TEXT NOT NULL,
                created_at TEXT NOT NULL,
                expires_at TEXT NOT NULL
            );
            """,
            "CREATE INDEX IF NOT EXISTS ix_risk_exceptions_key ON risk_exceptions(stable_finding_key);",
            "CREATE INDEX IF NOT EXISTS ix_findings_category ON findings(category);"
        ];
    }

    private static IReadOnlyList<string> SchemaV2Statements()
    {
        return
        [
            "ALTER TABLE findings ADD COLUMN decision TEXT NOT NULL DEFAULT 'Investigate';",
            "ALTER TABLE findings ADD COLUMN object_display TEXT NOT NULL DEFAULT '';",
            "ALTER TABLE findings ADD COLUMN last_run_id TEXT NULL;",
            "ALTER TABLE findings ADD COLUMN seen_count INTEGER NOT NULL DEFAULT 1;",
            """
            CREATE TABLE IF NOT EXISTS run_inventory (
                run_id TEXT NOT NULL,
                object_type TEXT NOT NULL,
                total_count INTEGER NOT NULL,
                capability_state TEXT NOT NULL,
                message TEXT NULL,
                PRIMARY KEY (run_id, object_type),
                FOREIGN KEY (run_id) REFERENCES runs(run_id) ON DELETE CASCADE
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS run_metrics (
                run_id TEXT NOT NULL,
                metric_key TEXT NOT NULL,
                metric_value INTEGER NOT NULL,
                PRIMARY KEY (run_id, metric_key),
                FOREIGN KEY (run_id) REFERENCES runs(run_id) ON DELETE CASCADE
            );
            """
        ];
    }

    private static async Task<int> GetCurrentVersionAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        var tableExists = await ExecuteScalarAsync<long>(
            connection,
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'schema_migrations';",
            cancellationToken);

        if (tableExists == 0)
        {
            return 0;
        }

        return await ExecuteScalarAsync<int>(
            connection,
            "SELECT COALESCE(MAX(version), 0) FROM schema_migrations;",
            cancellationToken);
    }

    private static async Task ExecuteNonQueryAsync(
        SqliteConnection connection,
        string commandText,
        CancellationToken cancellationToken,
        SqliteTransaction? transaction = null)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = commandText;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<T> ExecuteScalarAsync<T>(
        SqliteConnection connection,
        string commandText,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        var value = await command.ExecuteScalarAsync(cancellationToken);
        if (value is null || value is DBNull)
        {
            return default!;
        }

        return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
    }

    private static string InPlaceholders(int count) =>
        string.Join(", ", Enumerable.Range(0, count).Select(i => $"$p{i}"));

    private static void AddInParameters(SqliteCommand command, IReadOnlyList<string> values)
    {
        for (var i = 0; i < values.Count; i++)
        {
            command.Parameters.AddWithValue($"$p{i}", values[i]);
        }
    }

    private static TEnum ParseEnum<TEnum>(string value, TEnum fallback) where TEnum : struct =>
        Enum.TryParse<TEnum>(value, out var parsed) ? parsed : fallback;
}
