using ProductionSupportCopilot.Domain.Common;

namespace ProductionSupportCopilot.Domain.Ingestion;

public enum IngestionStatus { Pending, Processing, Completed, Failed, DeadLettered }

public sealed class IngestionJob : Entity
{
    private IngestionJob() { }

    public IngestionJob(Guid tenantId, string eventId, string externalDocumentId)
    {
        TenantId = tenantId;
        EventId = eventId;
        ExternalDocumentId = externalDocumentId;
    }

    public Guid TenantId { get; private set; }
    public string EventId { get; private set; } = string.Empty;
    public string ExternalDocumentId { get; private set; } = string.Empty;
    public IngestionStatus Status { get; private set; } = IngestionStatus.Pending;
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }

    public void Start() { Status = IngestionStatus.Processing; Attempts++; MarkUpdated(); }
    public void Complete() { Status = IngestionStatus.Completed; LastError = null; MarkUpdated(); }
    public void Fail(string error, bool deadLettered) { Status = deadLettered ? IngestionStatus.DeadLettered : IngestionStatus.Failed; LastError = error; MarkUpdated(); }
}
