using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProductionSupportCopilot.Application.Llm;

namespace ProductionSupportCopilot.Infrastructure.Llm;

public sealed class OllamaLlmProvider(HttpClient client, string model) : ILlmProvider
{
    public string Name => $"ollama:{model}";

    public async Task<LlmAnswer> GenerateAsync(LlmRequest request, CancellationToken cancellationToken = default)
    {
        var evidence = string.Join("\n\n", request.Evidence.Select(x => $"CHUNK_ID={x.ChunkId}\nTITLE={x.Title}\n{x.Content}"));
        var prompt = $"{request.SystemInstruction}\nReturn JSON only. citedChunkIds must contain only supplied IDs.\nQUESTION:\n{request.Question}\nEVIDENCE:\n{evidence}";
        var payload = new
        {
            model,
            stream = false,
            messages = new[] { new { role = "user", content = prompt } },
            format = new
            {
                type = "object",
                properties = new
                {
                    answer = new { type = "string" },
                    citedChunkIds = new { type = "array", items = new { type = "string" } },
                    hasSufficientEvidence = new { type = "boolean" }
                },
                required = new[] { "answer", "citedChunkIds", "hasSufficientEvidence" }
            },
            options = new { temperature = 0.1 }
        };
        using var response = await client.PostAsJsonAsync("/api/chat", payload, cancellationToken);
        response.EnsureSuccessStatusCode();
        var outer = await response.Content.ReadFromJsonAsync<OllamaResponse>(cancellationToken) ?? throw new InvalidOperationException("Ollama returned an empty response.");
        var parsed = JsonSerializer.Deserialize<StructuredAnswer>(outer.Message.Content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new JsonException("Ollama structured output was invalid.");
        var allowed = request.Evidence.Select(x => x.ChunkId).ToHashSet();
        var citations = parsed.CitedChunkIds.Select(x => Guid.TryParse(x, out var id) ? id : Guid.Empty).Where(allowed.Contains).Distinct().ToList();
        return new LlmAnswer(parsed.Answer, citations, parsed.HasSufficientEvidence && citations.Count > 0, Name, outer.PromptEvalCount, outer.EvalCount);
    }

    private sealed record OllamaResponse(
        OllamaMessage Message,
        [property: JsonPropertyName("prompt_eval_count")] int PromptEvalCount,
        [property: JsonPropertyName("eval_count")] int EvalCount);
    private sealed record OllamaMessage(string Content);
    private sealed record StructuredAnswer(string Answer, IReadOnlyList<string> CitedChunkIds, bool HasSufficientEvidence);
}
