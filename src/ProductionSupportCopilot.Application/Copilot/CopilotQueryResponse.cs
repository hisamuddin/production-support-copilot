namespace ProductionSupportCopilot.Application.Copilot;

public sealed record CopilotQueryResponse(
    string Answer,
    IReadOnlyCollection<Citation> Citations,
    bool IsGrounded,
    string Status,
    string Model = "none",
    int InputTokens = 0,
    int OutputTokens = 0,
    long LatencyMs = 0);

public sealed record Citation(string DocumentId, string ChunkId, string Title, int Ordinal = 0, double Score = 0);
