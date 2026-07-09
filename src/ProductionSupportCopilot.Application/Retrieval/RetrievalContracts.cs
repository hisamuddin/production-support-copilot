namespace ProductionSupportCopilot.Application.Retrieval;

public sealed record RetrievalQuery(string TenantKey, string Query, int TopK = 5, string? Classification = null);
public sealed record RetrievedPassage(Guid DocumentId, Guid ChunkId, string Title, string Content, int Ordinal, double LexicalScore, double SemanticScore, double CombinedScore, string SourceType);

public interface IEmbeddingProvider
{
    string Name { get; }
    int Dimensions { get; }
    Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default);
}

public interface IHybridRetriever
{
    Task<IReadOnlyList<RetrievedPassage>> SearchAsync(RetrievalQuery query, CancellationToken cancellationToken = default);
}
