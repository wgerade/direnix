namespace Direnix.Core.Identity;

public enum AppRole
{
    LocalAdmin,
    CollectorOperator,
    SecurityAnalyst,
    RiskManager,
    Auditor,
    ExecutiveViewer,
    ReadOnlyTechnical
}

public static class AppRolePolicy
{
    public static bool CanConfigureUsers(AppRole role) => role == AppRole.LocalAdmin;

    public static bool CanRunCollection(AppRole role) =>
        role is AppRole.LocalAdmin or AppRole.CollectorOperator;

    public static bool CanViewTechnicalFindings(AppRole role) =>
        role is AppRole.LocalAdmin
            or AppRole.CollectorOperator
            or AppRole.SecurityAnalyst
            or AppRole.Auditor
            or AppRole.ReadOnlyTechnical;

    public static bool CanAcceptRisk(AppRole role) =>
        role is AppRole.LocalAdmin or AppRole.RiskManager;

    public static bool CanViewAudit(AppRole role) =>
        role is AppRole.LocalAdmin or AppRole.Auditor;
}
