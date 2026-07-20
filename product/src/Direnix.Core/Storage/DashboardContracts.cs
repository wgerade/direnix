using Direnix.Core.Findings;
using Direnix.Core.Rules;

namespace Direnix.Core.Storage;

/// <summary>Registro resumido de um run persistido.</summary>
public sealed record RunRecord(
    string RunId,
    string CollectionType,
    string CoverageMode,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    string? DomainDn,
    string? DomainName,
    string CollectorVersion);

/// <summary>Metadados de execucao de um run (quem/como).</summary>
public sealed record RunMetadata(string? Operator, string? ExecutedAs, string? CredentialPrincipal);

/// <summary>Resumo de um run para o historico de avaliacoes.</summary>
public sealed record RunSummary(
    string RunId,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    string CoverageMode,
    string? Operator,
    string? ExecutedAs,
    string? CredentialPrincipal,
    int ObjectCount,
    int FindingCount);

/// <summary>
/// Estado atual de inventario por tipo (acumulado entre runs). Nao zera em coleta
/// parcial: mostra o ultimo valor conhecido e se foi observado no run mais recente.
/// </summary>
public sealed record InventoryState(
    string ObjectType,
    int TotalCount,
    DateTimeOffset? LastObservedAt,
    bool IsCurrent);

/// <summary>Uma observacao de um objeto em um run (historico por objeto).</summary>
public sealed record ObjectHistoryEntry(
    string RunId,
    DateTimeOffset ObservedAt,
    string AttributesJson);

/// <summary>Metrica chave/valor de um run (riskScore, findings, etc.).</summary>
public sealed record MetricValue(string Key, int Value);

/// <summary>Linha de finding para listagem no dashboard.</summary>
public sealed record FindingRow(
    string StableFindingKey,
    string RuleId,
    string Title,
    FindingCategory Category,
    Severity Severity,
    FindingDecision Decision,
    FindingStatus Status,
    int BusinessRiskScore,
    string ObjectDisplay,
    DateTimeOffset FirstSeen,
    DateTimeOffset LastSeen,
    string EvidenceJson,
    string? LastRunId,
    string ObjectKey,
    string? ResolutionReason);

/// <summary>Contagem de findings ativos por categoria.</summary>
public sealed record CategoryCount(FindingCategory Category, int Count);

/// <summary>Indicador operacional persistido (do run mais recente), com o drill-down.</summary>
public sealed record IndicatorResultRow(
    string Id,
    string Title,
    string Category,
    bool IsCustom,
    int Count,
    IReadOnlyList<Indicators.IndicatorItem> Items);

/// <summary>Excecao (aceite de risco) registrada para um finding.</summary>
public sealed record RiskExceptionRecord(
    string ExceptionId,
    string StableFindingKey,
    string Owner,
    string Justification,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt);

/// <summary>Entrada para criar uma excecao.</summary>
public sealed record RiskExceptionInput(
    string StableFindingKey,
    string Owner,
    string Justification,
    int ValidForDays);

/// <summary>Excecao com os dados do risco que ela cobre (para a aba de aceites).</summary>
public sealed record RiskExceptionView(
    string ExceptionId,
    string StableFindingKey,
    string Owner,
    string Justification,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    string RuleId,
    string Title,
    string Category,
    string Severity,
    string ObjectDisplay);

/// <summary>Contagem de findings ativos por severidade.</summary>
public sealed record SeverityCount(Severity Severity, int Count);

/// <summary>Estado consolidado para o painel.</summary>
public sealed record DashboardData(
    RunRecord? LatestRun,
    IReadOnlyList<MetricValue> Metrics,
    IReadOnlyList<InventoryState> Inventory,
    IReadOnlyList<SeverityCount> SeverityBreakdown,
    IReadOnlyList<CategoryCount> CategoryBreakdown,
    int ActiveFindings,
    int IdentityScore,
    int Tier0Score,
    string Health);

/// <summary>Evento de mudança (Timeline / Morning View).</summary>
public sealed record ChangeEventRow(
    string RunId,
    DateTimeOffset ObservedAt,
    string ObjectKey,
    string ObjectType,
    string ObjectDisplay,
    string ChangeType,
    string Attribute,
    string? OldValue,
    string? NewValue,
    string Severity);

/// <summary>Contagem de mudanças por tipo num período (Morning View).</summary>
public sealed record ChangeCount(string ChangeType, int Count);

/// <summary>Resultado de busca global.</summary>
public sealed record SearchHit(string ObjectKey, string ObjectType, string Display, string Subtitle);

/// <summary>Par rótulo/valor para a página de objeto.</summary>
public sealed record ObjectField(string Label, string Value);

/// <summary>Visão consolidada de um objeto (uma página só).</summary>
public sealed record ObjectDetail(
    string ObjectKey,
    string ObjectType,
    string Display,
    string DistinguishedName,
    DateTimeOffset? LastObservedAt,
    IReadOnlyList<ObjectField> Fields,
    IReadOnlyList<FindingRow> Findings,
    IReadOnlyList<ChangeEventRow> Changes);
