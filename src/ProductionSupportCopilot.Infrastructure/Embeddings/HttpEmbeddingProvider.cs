using System.Net.Http.Json;
using ProductionSupportCopilot.Application.Retrieval;

namespace ProductionSupportCopilot.Infrastructure.Embeddings;

public sealed class HttpEmbeddingProvider(HttpClient client) : IEmbeddingProvider
{
    public string Name => "sentence-transformers/all-MiniLM-L6-v2";
    public int Dimensions => 384;

    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        using var response = await client.PostAsJsonAsync("/embed", new { texts = new[] { text } }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken) ?? throw new InvalidOperationException("Embedding service returned no body.");
        if (payload.Embeddings.Count != 1 || payload.Embeddings[0].Length != Dimensions) throw new InvalidOperationException("Embedding response has unexpected dimensions.");
        return payload.Embeddings[0];
    }

    private sealed record EmbeddingResponse(IReadOnlyList<float[]> Embeddings, string Model, int Dimensions);
}
