namespace Direnix.Core.Findings;

/// <summary>
/// Severidade tecnica (`docs/DIRENIX_RULES_AND_INDICATORS.md` secao 7.1).
/// </summary>
public enum Severity
{
    Info,
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Categoria operacional do risco (agrupamento de mercado).
/// </summary>
public enum FindingCategory
{
    Hygiene,
    PrivilegedAccess,
    Hardening,
    Infrastructure,
    Governance
}

/// <summary>
/// Decisao recomendada por regra (secao 6 do doc de regras).
/// </summary>
public enum FindingDecision
{
    CleanUp,
    Adjust,
    Implement,
    Investigate,
    Monitor,
    Decommission,
    AcceptRisk,
    NeedsEvidence,
    ReadyForCleanup,
    GenerateScript
}

/// <summary>
/// Estado operacional persistido do finding (alinha com `MVP_IMPLEMENTATION_PLAN.md`
/// fase 2 e com a tabela `findings.status`).
/// </summary>
public enum FindingStatus
{
    New,
    Open,
    Resolved,
    Recurring,
    AcceptedRisk,
    CapabilityMissing,
    NeedsEvidence,
    ReadyForCleanup,
    ScriptGenerated,
    ValidationPending
}
