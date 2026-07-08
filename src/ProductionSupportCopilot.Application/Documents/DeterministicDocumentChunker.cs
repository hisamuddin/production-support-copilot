namespace ProductionSupportCopilot.Application.Documents;

/// <summary>Splits text on paragraph/sentence boundaries with deterministic overlap.</summary>
public sealed class DeterministicDocumentChunker : IDocumentChunker
{
    public IReadOnlyList<ChunkDraft> Chunk(string content, int targetCharacters = 1200, int overlapCharacters = 180)
    {
        if (string.IsNullOrWhiteSpace(content)) return [];
        if (targetCharacters < 200) throw new ArgumentOutOfRangeException(nameof(targetCharacters));
        if (overlapCharacters < 0 || overlapCharacters >= targetCharacters) throw new ArgumentOutOfRangeException(nameof(overlapCharacters));

        var normalized = content.Replace("\r\n", "\n").Trim();
        var chunks = new List<ChunkDraft>();
        var start = 0;
        while (start < normalized.Length)
        {
            var desiredEnd = Math.Min(start + targetCharacters, normalized.Length);
            var end = desiredEnd == normalized.Length ? desiredEnd : FindBoundary(normalized, start, desiredEnd);
            if (end <= start) end = desiredEnd;

            var text = normalized[start..end].Trim();
            if (text.Length > 0) chunks.Add(new ChunkDraft(chunks.Count, text, start, end));
            if (end >= normalized.Length) break;
            start = Math.Max(start + 1, end - overlapCharacters);
        }

        return chunks;
    }

    private static int FindBoundary(string text, int start, int desiredEnd)
    {
        var minimum = start + ((desiredEnd - start) * 2 / 3);
        for (var i = desiredEnd; i >= minimum; i--)
        {
            if (i + 1 < text.Length && text[i] == '\n' && text[i + 1] == '\n') return i;
            if (text[i - 1] is '.' or '?' or '!' or '\n') return i;
        }
        return desiredEnd;
    }
}
