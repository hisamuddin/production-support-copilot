namespace ProductionSupportCopilot.Domain.Ingestion;

public enum IngestionJobStatus
{
    Pending = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4
}

public sealed record IngestionJob(
    Guid Id,
    Guid TenantId,
    Guid DocumentId,
    string IdempotencyKey,
    IngestionJobStatus Status,
    int AttemptCount,
    string? FailureReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
