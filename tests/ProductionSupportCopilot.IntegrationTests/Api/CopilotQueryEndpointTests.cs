using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ProductionSupportCopilot.IntegrationTests.Api;

public sealed class CopilotQueryEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CopilotQueryEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory
            .WithWebHostBuilder(builder => builder.UseEnvironment("Testing"))
            .CreateClient();
    }

    [Fact]
    public async Task Query_WithValidQuestion_ReturnsSafeUngroundedResponse()
    {
        using var response = await _client.PostAsJsonAsync(
            "/api/v1/copilot/query",
            new { question = "Why are booking updates delayed?", tenantId = "demo-tenant" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Correlation-ID"));

        var body = await response.Content.ReadFromJsonAsync<CopilotResponse>();
        Assert.NotNull(body);
        Assert.False(body.IsGrounded);
        Assert.Empty(body.Citations);
        Assert.Equal("retrieval_not_configured", body.Status);
    }

    [Fact]
    public async Task Query_WithBlankQuestion_ReturnsValidationProblem()
    {
        using var response = await _client.PostAsJsonAsync(
            "/api/v1/copilot/query",
            new { question = " ", tenantId = "demo-tenant" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed record CopilotResponse(
        string Answer,
        IReadOnlyCollection<Citation> Citations,
        bool IsGrounded,
        string Status);

    private sealed record Citation(string DocumentId, string ChunkId, string Title);
}
