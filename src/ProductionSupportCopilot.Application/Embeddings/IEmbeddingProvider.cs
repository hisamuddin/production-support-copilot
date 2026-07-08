namespace ProductionSupportCopilot.Application.Embeddings;

public interface IEmbeddingProvider
{
    Task<EmbeddingBatch> GenerateAsync(
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken = default);
}

public sealed record EmbeddingBatch(
    string Model,
    int Dimensions,
    IReadOnlyList<float[]> Vectors);
