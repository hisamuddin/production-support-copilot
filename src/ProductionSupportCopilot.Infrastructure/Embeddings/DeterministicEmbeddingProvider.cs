using System.Security.Cryptography;
using System.Text;
using ProductionSupportCopilot.Application.Retrieval;

namespace ProductionSupportCopilot.Infrastructure.Embeddings;

/// <summary>Offline fallback for tests/demo. It is deterministic but not a semantic ML model.</summary>
public sealed class DeterministicEmbeddingProvider : IEmbeddingProvider
{
    public string Name => "deterministic-hash-fallback";
    public int Dimensions => 384;

    public Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var vector = new float[Dimensions];
        foreach (var token in Tokenize(text))
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            var index = BitConverter.ToUInt16(hash, 0) % Dimensions;
            vector[index] += (hash[2] & 1) == 0 ? 1f : -1f;
        }
        Normalize(vector);
        return Task.FromResult(vector);
    }

    private static IEnumerable<string> Tokenize(string text) => text.ToLowerInvariant().Split([' ', '\r', '\n', '\t', '.', ',', ':', ';', '-', '/', '(', ')'], StringSplitOptions.RemoveEmptyEntries);
    private static void Normalize(float[] values)
    {
        var norm = MathF.Sqrt(values.Sum(x => x * x));
        if (norm == 0) return;
        for (var i = 0; i < values.Length; i++) values[i] /= norm;
    }
}
