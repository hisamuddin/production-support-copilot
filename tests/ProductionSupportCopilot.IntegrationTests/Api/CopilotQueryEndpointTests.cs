using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProductionSupportCopilot.Application.Retrieval;

namespace ProductionSupportCopilot.IntegrationTests.Api;

public sealed class CopilotApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.RemoveAll<IHybridRetriever>();
            services.AddSingleton<IHybridRetriever, FakeRetriever>();
        });
    }

    private sealed class FakeRetriever : IHybridRetriever
    {
        public Task<IReadOnlyList<RetrievedPassage>> SearchAsync(RetrievalQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RetrievedPassage>>([new(Guid.NewGuid(), Guid.NewGuid(), "Consumer Lag Runbook", "Consumer lag can delay booking status updates. Check lag and downstream timeouts.", 0, 1, .8, .03, "runbook")]);
    }
}

public sealed class CopilotQueryEndpointTests(CopilotApiFactory factory) : IClassFixture<CopilotApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Query_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/copilot/query", new { question = "Why delayed?", tenantId = "demo-tenant" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Query_WithTokenAndEvidence_ReturnsGroundedCitations()
    {
        await AuthenticateAsync();
        var response = await _client.PostAsJsonAsync("/api/v1/copilot/query", new { question = "Why are booking updates delayed?", tenantId = "demo-tenant" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Correlation-ID"));
        var body = await response.Content.ReadFromJsonAsync<CopilotResponse>();
        Assert.NotNull(body);
        Assert.True(body.IsGrounded);
        Assert.NotEmpty(body.Citations);
        Assert.Equal("grounded", body.Status);
    }

    [Fact]
    public async Task Query_ForDifferentTenant_ReturnsForbidden()
    {
        await AuthenticateAsync();
        var response = await _client.PostAsJsonAsync("/api/v1/copilot/query", new { question = "Why delayed?", tenantId = "other-tenant" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task AuthenticateAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/demo-token", new { userId = "tester", tenantId = "demo-tenant", requestedRoles = new[] { "support", "operator" } });
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
    }

    private sealed record TokenResponse(string AccessToken);
    private sealed record CopilotResponse(string Answer, IReadOnlyCollection<Citation> Citations, bool IsGrounded, string Status);
    private sealed record Citation(string DocumentId, string ChunkId, string Title);
}
