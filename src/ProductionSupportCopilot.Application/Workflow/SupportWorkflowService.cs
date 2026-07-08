using System.Text.Json;
using ProductionSupportCopilot.Application.Copilot;
using ProductionSupportCopilot.Application.Tools;

namespace ProductionSupportCopilot.Application.Workflow;

public sealed record WorkflowRequest(string Question, string TenantId, string UserId, IReadOnlySet<string> Roles);
public interface ISupportWorkflowService { Task<WorkflowResult> RunAsync(WorkflowRequest request, CancellationToken cancellationToken = default); }

/// <summary>Explicit, inspectable state machine. The LLM never receives authority to execute tools directly.</summary>
public sealed class SupportWorkflowService(ICopilotQueryService copilot, IToolRegistry tools) : ISupportWorkflowService
{
    public async Task<WorkflowResult> RunAsync(WorkflowRequest request, CancellationToken cancellationToken = default)
    {
        var trace = new List<WorkflowTraceItem>();
        Add(WorkflowStage.Understand, "Classify the request using deterministic safety-first routing.");

        var (toolName, arguments) = SelectTool(request.Question);
        if (toolName is not null)
        {
            Add(WorkflowStage.DecideTool, $"Selected tool '{toolName}'.");
            var tool = tools.Find(toolName) ?? throw new InvalidOperationException("Configured tool was not registered.");
            Add(WorkflowStage.ExecuteTool, tool.IsConsequential ? "Tool will create approval request only." : "Execute read-only mock tool.");
            using var json = JsonDocument.Parse(arguments);
            var result = await tool.ExecuteAsync(json.RootElement, new ToolContext(request.TenantId, request.UserId, request.Roles), cancellationToken);
            if (result.RequiresApproval)
            {
                Add(WorkflowStage.AwaitApproval, "Consequential action paused for human decision.");
                return new WorkflowResult(result.Summary, WorkflowStage.AwaitApproval, trace, result.ApprovalId);
            }
            Add(WorkflowStage.Complete, "Returned tool evidence without model invention.");
            return new WorkflowResult(result.Summary + "\n" + JsonSerializer.Serialize(result.Data), WorkflowStage.Complete, trace);
        }

        Add(WorkflowStage.Retrieve, "No live tool required; run tenant-filtered retrieval.");
        Add(WorkflowStage.Synthesize, "Generate a grounded response with citations.");
        var answer = await copilot.QueryAsync(new CopilotQueryRequest(request.Question, request.TenantId), cancellationToken);
        Add(WorkflowStage.Complete, $"Copilot completed with status '{answer.Status}'.");
        return new WorkflowResult(answer.Answer, WorkflowStage.Complete, trace);

        void Add(WorkflowStage stage, string detail) => trace.Add(new WorkflowTraceItem(stage, DateTimeOffset.UtcNow, detail));
    }

    private static (string? Tool, string ArgumentsJson) SelectTool(string question)
    {
        var q = question.ToLowerInvariant();
        if (q.Contains("replay") && (q.Contains("dlq") || q.Contains("dead letter"))) return ("replay_dead_letter", "{\"topic\":\"support.document.ingestion.dlq.v1\",\"maxEvents\":10}");
        if (q.Contains("consumer lag") || q.Contains("kafka lag")) return ("get_consumer_lag", "{\"consumerGroup\":\"status-indexer\"}");
        if (q.Contains("healthy now") || q.Contains("current health")) return ("get_service_health", "{\"service\":\"booking-api\"}");
        if (q.Contains("booking") && q.Any(char.IsDigit)) return ("get_booking_status", "{\"bookingId\":\"demo-1001\"}");
        return (null, "{}");
    }
}
