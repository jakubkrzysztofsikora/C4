namespace C4.Shared.Kernel.Contracts;

public sealed record DriftRunRecord(
    DateTime? LastRunAtUtc,
    DriftRunStatus Status,
    string? Error = null,
    int DriftedCount = 0);

public enum DriftRunStatus
{
    NotRun,
    Running,
    Completed,
    Failed
}
