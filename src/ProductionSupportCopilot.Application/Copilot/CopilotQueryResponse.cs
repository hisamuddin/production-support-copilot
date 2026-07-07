namespace ProductionSupportCopilot.Application.Copilot;

public sealed record CopilotQueryResponse(
    string Answer,
    IReadOnlyCollection<Citation> Citations,
    bool IsGrounded,
    string Status);

public sealed record Citation(string DocumentId, string ChunkId, string Title);
