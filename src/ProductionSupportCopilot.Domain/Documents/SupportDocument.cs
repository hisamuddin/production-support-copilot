namespace ProductionSupportCopilot.Domain.Documents;

public sealed class SupportDocument
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Title { get; private set; }
    public DocumentSourceType SourceType { get; private set; }
    public string SourceReference { get; private set; }
    public int Version { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private SupportDocument()
    {
        Title = string.Empty;
        SourceReference = string.Empty;
    }

    public SupportDocument(
        Guid id,
        Guid tenantId,
        string title,
        DocumentSourceType sourceType,
        string sourceReference,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty) throw new ArgumentException("Document ID is required.", nameof(id));
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(sourceReference)) throw new ArgumentException("Source reference is required.", nameof(sourceReference));

        Id = id;
        TenantId = tenantId;
        Title = title.Trim();
        SourceType = sourceType;
        SourceReference = sourceReference.Trim();
        Version = 1;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }
}
