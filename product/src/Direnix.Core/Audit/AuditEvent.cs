namespace Direnix.Core.Audit;

public sealed record AuditEvent(
    DateTimeOffset Timestamp,
    string ActorUserId,
    string ActorRole,
    string Action,
    string TargetType,
    string TargetId,
    string SourceIp,
    string HostName,
    string Result,
    IReadOnlyDictionary<string, string> Details);
