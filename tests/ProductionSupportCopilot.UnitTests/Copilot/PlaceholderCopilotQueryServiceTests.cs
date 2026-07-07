using ProductionSupportCopilot.Application.Copilot;

namespace ProductionSupportCopilot.UnitTests.Copilot;

public sealed class PlaceholderCopilotQueryServiceTests
{
    [Fact]
    public async Task QueryAsync_WhenRetrievalIsUnavailable_AbstainsWithoutCitations()
    {
        var sut = new PlaceholderCopilotQueryService();

        var result = await sut.QueryAsync(new CopilotQueryRequest("Why are booking updates delayed?"));

        Assert.False(result.IsGrounded);
        Assert.Empty(result.Citations);
        Assert.Equal("retrieval_not_configured", result.Status);
    }
}
