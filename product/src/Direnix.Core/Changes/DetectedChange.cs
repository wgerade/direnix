using Direnix.Core.Findings;

namespace Direnix.Core.Changes;

/// <summary>
/// Uma mudança detectada por <see cref="ChangeDetector"/>, antes de ser associada a
/// um objeto/run/timestamp e persistida.
/// </summary>
public sealed record DetectedChange(
    ChangeType Type,
    string Attribute,
    string? OldValue,
    string? NewValue,
    Severity Severity);
