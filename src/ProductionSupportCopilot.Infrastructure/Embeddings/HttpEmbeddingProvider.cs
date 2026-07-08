using System.Net.Http.Json;
using ProductionSupportCopilot.Application.Embeddings;

namespace ProductionSupportCopilot.Infrastructure.Embeddings;

public sealed class HttpEmbeddingProvider(HttpClient httpClient) : IEmbeddingProvider
{
    public async Task<EmbeddingBatch> GenerateAsync(
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken = default)
    {
        if (inputs.Count is < 1 or > 32)
        {
            throw new ArgumentOutOfRangeException(nameof(inputs), "Embedding batch size must be between 1 and 32.");
        }

        if (inputs.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Embedding inputs cannot be blank.", nameof(inputs));
        }

        using var response = await httpClient.PostAsJsonAsync(
            "v1/embeddings",
            new EmbeddingRequest(inputs),
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Embedding service returned an empty response.");

        var orderedVectors = payload.Data
            .OrderBy(item => item.Index)
            .Select(item => item.Embedding)
            .ToArray();

        if (orderedVectors.Length != inputs.Count
            || orderedVectors.Any(vector => vector.Length != payload.Dimensions))
        {
            throw new InvalidOperationException("Embedding service returned an invalid vector shape.");
        }

        return new EmbeddingBatch(payload.Model, payload.Dimensions, orderedVectors);
    }

    private sealed record EmbeddingRequest(IReadOnlyList<string> Inputs);

    private sealed record EmbeddingResponse(
        string Model,
        int Dimensions,
        IReadOnlyList<EmbeddingItem> Data);

    private sealed record EmbeddingItem(int Index, float[] Embedding);
}
