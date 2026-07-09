using ProductionSupportCopilot.Application.Copilot;
using ProductionSupportCopilot.Application.Tools;
using ProductionSupportCopilot.Application.Workflow;

namespace ProductionSupportCopilot.UnitTests.Workflow;

public sealed class SupportWorkflowServiceTests
{
    [Fact]
    public async Task Run_CurrentHealth_UsesReadOnlyToolAndCompletes()
    {
        var registry = new ToolRegistry([new ServiceHealthTool()]);
        var sut = new SupportWorkflowService(new FakeCopilot(), registry);
        var result = await sut.RunAsync(new WorkflowRequest("Is Booking API healthy now?", "demo-tenant", "user", new HashSet<string> { "support" }));
        Assert.Equal(WorkflowStage.Complete, result.FinalStage);
        Assert.Contains(result.Trace, x => x.Stage == WorkflowStage.ExecuteTool);
    }

    private sealed class FakeCopilot : ICopilotQueryService
    {
        public Task<CopilotQueryResponse> QueryAsync(CopilotQueryRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new CopilotQueryResponse("fallback", [], false, "test"));
    }
}
