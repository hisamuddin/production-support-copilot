using System.Text.Json;
using ProductionSupportCopilot.Application.Workflow;

namespace ProductionSupportCopilot.Application.Tools;

public sealed class ReplayDeadLetterTool(IApprovalService approvals) : ISupportTool
{
    public string Name => "replay_dead_letter";
    public string Description => "Requests approval for a simulated DLQ replay. It never executes immediately.";
    public bool IsConsequential => true;

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments, ToolContext context, CancellationToken cancellationToken = default)
    {
        if (!context.Roles.Contains("operator")) return new ToolResult(false, "Operator role is required.", new { error = "forbidden" });
        var approvalId = await approvals.RequestAsync(context.TenantKey, Name, arguments.GetRawText(), context.UserId, cancellationToken);
        return new ToolResult(true, "Replay is paused until an authorized human approves it.", new { state = "awaiting_approval" }, true, approvalId);
    }
}
