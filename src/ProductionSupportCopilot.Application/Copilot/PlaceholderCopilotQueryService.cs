namespace ProductionSupportCopilot.Application.Copilot;

public sealed class PlaceholderCopilotQueryService : ICopilotQueryService
{
    public Task<CopilotQueryResponse> QueryAsync(
        CopilotQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var response = new CopilotQueryResponse(
            Answer: "Retrieval is not connected yet. No grounded answer is available.",
            Citations: [],
            IsGrounded: false,
            Status: "retrieval_not_configured");

        return Task.FromResult(response);
    }
}
