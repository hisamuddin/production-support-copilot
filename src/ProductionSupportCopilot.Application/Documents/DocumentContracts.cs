namespace ProductionSupportCopilot.Application.Documents;

public sealed record ChunkDraft(int Ordinal, string Content, int StartCharacter, int EndCharacter);
public sealed record IngestDocumentCommand(string EventId, string TenantKey, string ExternalId, string Title, string Content, string SourceType = "runbook", string Classification = "internal");
public sealed record IngestionResult(Guid JobId, string Status, int ChunkCount, bool WasDuplicate, string? Error = null);

public interface IDocumentChunker
{
    IReadOnlyList<ChunkDraft> Chunk(string content, int targetCharacters = 1200, int overlapCharacters = 180);
}

public interface IDocumentIngestionService
{
    Task<IngestionResult> IngestAsync(IngestDocumentCommand command, CancellationToken cancellationToken = default);
}

public interface IIngestionEventPublisher
{
    Task PublishAsync(IngestDocumentCommand command, CancellationToken cancellationToken = default);
}
