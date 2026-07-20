namespace Direnix.Core.Audit;

public interface IAuditSink
{
    ValueTask WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken);
}
