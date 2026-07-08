namespace ProductionSupportCopilot.Application.Workflow;

public enum WorkflowStage { Understand, Retrieve, DecideTool, ExecuteTool, Synthesize, AwaitApproval, Complete, Rejected }
public sealed record WorkflowTraceItem(WorkflowStage Stage, DateTimeOffset AtUtc, string Detail);
public sealed record WorkflowResult(string Answer, WorkflowStage FinalStage, IReadOnlyList<WorkflowTraceItem> Trace, Guid? ApprovalId = null);

public interface IApprovalService
{
    Task<Guid> RequestAsync(string tenantKey, string action, string argumentsJson, string requestedBy, CancellationToken cancellationToken = default);
    Task<bool> DecideAsync(Guid approvalId, bool approve, string decidedBy, CancellationToken cancellationToken = default);
}
