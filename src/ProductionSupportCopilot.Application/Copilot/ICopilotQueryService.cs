namespace ProductionSupportCopilot.Application.Copilot;

public interface ICopilotQueryService
{
    Task<CopilotQueryResponse> QueryAsync(
        CopilotQueryRequest request,
        CancellationToken cancellationToken = default);
}
