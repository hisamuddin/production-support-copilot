namespace ProductionSupportCopilot.Application.Retrieval;

public interface IDocumentSearchService
{
    Task<IReadOnlyList<DocumentSearchResult>> SearchAsync(
        Guid tenantId,
        string query,
        int limit = 5,
        CancellationToken cancellationToken = default);
}

public sealed record DocumentSearchResult(
    Guid ChunkId,
    Guid DocumentId,
    string DocumentTitle,
    int Sequence,
    string Content,
    double Score);
