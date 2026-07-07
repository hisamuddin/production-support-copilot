namespace ProductionSupportCopilot.Domain.Documents;

public sealed record DocumentChunk(
    Guid Id,
    Guid TenantId,
    Guid DocumentId,
    int DocumentVersion,
    int Sequence,
    string Content,
    int StartOffset,
    int EndOffset);
