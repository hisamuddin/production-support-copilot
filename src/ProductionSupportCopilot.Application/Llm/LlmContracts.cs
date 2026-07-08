using ProductionSupportCopilot.Application.Retrieval;

namespace ProductionSupportCopilot.Application.Llm;

public sealed record LlmRequest(string Question, IReadOnlyList<RetrievedPassage> Evidence, string SystemInstruction);
public sealed record LlmAnswer(string Answer, IReadOnlyList<Guid> CitedChunkIds, bool HasSufficientEvidence, string Model, int InputTokens, int OutputTokens);

public interface ILlmProvider
{
    string Name { get; }
    Task<LlmAnswer> GenerateAsync(LlmRequest request, CancellationToken cancellationToken = default);
}

public sealed class ExtractiveDemoLlmProvider : ILlmProvider
{
    public string Name => "deterministic-extractive-demo";

    public Task<LlmAnswer> GenerateAsync(LlmRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request.Evidence.Count == 0)
            return Task.FromResult(new LlmAnswer("Mujhe authorized documents mein enough evidence nahi mila.", [], false, Name, Estimate(request.Question), 14));

        var selected = request.Evidence.Take(3).ToList();
        var bullets = selected.Select((x, i) => $"{i + 1}. {FirstUsefulSentence(x.Content)} [{x.Title}]");
        var answer = "Available evidence ke basis par:\n" + string.Join("\n", bullets) + "\n\nLive system state confirm karne ke liye relevant read-only tool use karein.";
        return Task.FromResult(new LlmAnswer(answer, selected.Select(x => x.ChunkId).ToList(), true, Name, Estimate(request.Question + string.Join(' ', selected.Select(x => x.Content))), Estimate(answer)));
    }

    private static string FirstUsefulSentence(string text)
    {
        var line = text.Split(['\n', '.', '?', '!'], StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).FirstOrDefault(x => x.Length > 20) ?? text.Trim();
        return line.Length <= 280 ? line : line[..277] + "...";
    }

    private static int Estimate(string text) => Math.Max(1, text.Length / 4);
}

public sealed class ResilientLlmProvider(ILlmProvider primary, ILlmProvider fallback) : ILlmProvider
{
    public string Name => $"resilient:{primary.Name}->{fallback.Name}";
    public async Task<LlmAnswer> GenerateAsync(LlmRequest request, CancellationToken cancellationToken = default)
    {
        Exception? last = null;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try { return await primary.GenerateAsync(request, cancellationToken); }
            catch (Exception ex) when (ex is not OperationCanceledException) { last = ex; await Task.Delay(TimeSpan.FromMilliseconds(100 * attempt), cancellationToken); }
        }
        var result = await fallback.GenerateAsync(request, cancellationToken);
        return result with { Model = $"{result.Model} (fallback after {last?.GetType().Name})" };
    }
}
