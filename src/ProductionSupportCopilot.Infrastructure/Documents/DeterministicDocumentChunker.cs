using ProductionSupportCopilot.Application.Documents;

namespace ProductionSupportCopilot.Infrastructure.Documents;

public sealed class DeterministicDocumentChunker : IDocumentChunker
{
    public IReadOnlyList<TextChunk> Chunk(string content, ChunkingOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return [];
        }

        options ??= new ChunkingOptions();
        options.Validate();

        var normalized = NormalizeLineEndings(content).Trim();
        var chunks = new List<TextChunk>();
        var start = 0;

        while (start < normalized.Length)
        {
            var remaining = normalized.Length - start;
            var desiredEnd = start + Math.Min(options.MaximumCharacters, remaining);
            var end = FindNaturalBoundary(normalized, start, desiredEnd);

            var text = normalized[start..end].Trim();
            if (text.Length > 0)
            {
                var actualStart = normalized.IndexOf(text, start, StringComparison.Ordinal);
                chunks.Add(new TextChunk(chunks.Count, text, actualStart, actualStart + text.Length));
            }

            if (end >= normalized.Length)
            {
                break;
            }

            var nextStart = Math.Max(0, end - options.OverlapCharacters);
            start = MoveToWordBoundary(normalized, nextStart, end);

            if (start >= end)
            {
                start = end;
            }
        }

        return chunks;
    }

    private static string NormalizeLineEndings(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    private static int FindNaturalBoundary(string content, int start, int desiredEnd)
    {
        if (desiredEnd >= content.Length)
        {
            return content.Length;
        }

        var minimumBoundary = start + ((desiredEnd - start) / 2);
        var paragraphBreak = content.LastIndexOf("\n\n", desiredEnd, desiredEnd - minimumBoundary, StringComparison.Ordinal);
        if (paragraphBreak >= minimumBoundary)
        {
            return paragraphBreak;
        }

        var sentenceEnd = content.LastIndexOfAny(['.', '!', '?', '\n'], desiredEnd - 1, desiredEnd - minimumBoundary);
        if (sentenceEnd >= minimumBoundary)
        {
            return sentenceEnd + 1;
        }

        var whitespace = content.LastIndexOf(' ', desiredEnd - 1, desiredEnd - minimumBoundary);
        return whitespace >= minimumBoundary ? whitespace : desiredEnd;
    }

    private static int MoveToWordBoundary(string content, int candidate, int upperBound)
    {
        while (candidate < upperBound && candidate > 0 && !char.IsWhiteSpace(content[candidate - 1]))
        {
            candidate++;
        }

        while (candidate < content.Length && char.IsWhiteSpace(content[candidate]))
        {
            candidate++;
        }

        return candidate;
    }
}
