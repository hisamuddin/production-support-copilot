using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry;
using OpenTelemetry.Trace;
using ProductionSupportCopilot.Api.Middleware;
using ProductionSupportCopilot.Api.Observability;
using ProductionSupportCopilot.Application.Copilot;
using ProductionSupportCopilot.Application.Documents;
using ProductionSupportCopilot.Application.Llm;
using ProductionSupportCopilot.Application.Observability;
using ProductionSupportCopilot.Application.Retrieval;
using ProductionSupportCopilot.Application.Security;
using ProductionSupportCopilot.Application.Tools;
using ProductionSupportCopilot.Application.Workflow;
using ProductionSupportCopilot.Infrastructure;
using ProductionSupportCopilot.Infrastructure.Llm;
using ProductionSupportCopilot.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"))
{
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Logging.AddDebug();
    builder.Logging.AddFilter("Microsoft.AspNetCore.DataProtection", LogLevel.Error);
}

var jwtKey = builder.Configuration["Authentication:JwtKey"]
    ?? "development-only-key-change-me-32-characters";
var jwtIssuer = builder.Configuration["Authentication:Issuer"]
    ?? "production-support-copilot";

builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Production Support Copilot API",
        Version = "v1",
        Description = "Citation-grounded support copilot for event-driven .NET services."
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        }] = Array.Empty<string>()
    });
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = "support-copilot-api",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"))
{
    var keyDirectory = Path.Combine(builder.Environment.ContentRootPath, ".aspnet-data-protection-keys");
    Directory.CreateDirectory(keyDirectory);
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keyDirectory));
}

builder.Services.AddRateLimiter(options => options.AddFixedWindowLimiter("api", limiter =>
{
    limiter.PermitLimit = 60;
    limiter.Window = TimeSpan.FromMinutes(1);
    limiter.QueueLimit = 5;
}));

builder.Services.AddSingleton<RequestTelemetry>();
builder.Services.AddSingleton<IAiUsageRecorder>(sp => sp.GetRequiredService<RequestTelemetry>());
var telemetry = builder.Services.AddOpenTelemetry().WithTracing(tracing =>
{
    tracing
        .AddSource(CopilotTelemetry.SourceName)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation();
});
var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];
if (!string.IsNullOrWhiteSpace(otlpEndpoint))
{
    telemetry.WithTracing(tracing => tracing.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint)));
}

builder.Services.AddSingleton<IPromptInjectionGuard, PromptInjectionGuard>();
builder.Services.AddSingleton<ExtractiveDemoLlmProvider>();
builder.Services.AddHttpClient("ollama", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Llm:OllamaBaseUrl"] ?? "http://localhost:11434");
    client.Timeout = TimeSpan.FromSeconds(90);
});
builder.Services.AddTransient<OllamaLlmProvider>(sp => new OllamaLlmProvider(
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("ollama"),
    builder.Configuration["Llm:Model"] ?? "llama3.1:8b"));
builder.Services.AddScoped<ILlmProvider>(sp => builder.Configuration.GetValue("Llm:UseOllama", false)
    ? new ResilientLlmProvider(
        sp.GetRequiredService<OllamaLlmProvider>(),
        sp.GetRequiredService<ExtractiveDemoLlmProvider>())
    : sp.GetRequiredService<ExtractiveDemoLlmProvider>());

builder.Services.AddScoped<ICopilotQueryService, GroundedCopilotQueryService>();
builder.Services.AddScoped<ISupportWorkflowService, SupportWorkflowService>();
builder.Services.AddScoped<ISupportTool, ServiceHealthTool>();
builder.Services.AddScoped<ISupportTool, ConsumerLagTool>();
builder.Services.AddScoped<ISupportTool, BookingStatusTool>();
builder.Services.AddScoped<ISupportTool, ReplayDeadLetterTool>();
builder.Services.AddScoped<IToolRegistry, ToolRegistry>();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing") && app.Configuration.GetValue("Database:Initialize", false))
{
    await DatabaseInitializer.InitializeAsync(app.Services);
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestTelemetryMiddleware>();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Production Support Copilot API v1");
    options.DocumentTitle = "Production Support Copilot API";
});

app.MapHealthChecks("/healthz");

app.MapPost("/api/v1/auth/demo-token", (DemoTokenRequest request) =>
{
    var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, request.UserId),
        new("tenant", request.TenantId)
    };
    claims.AddRange(request.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

    var token = new JwtSecurityToken(
        jwtIssuer,
        "support-copilot-api",
        claims,
        expires: DateTime.UtcNow.AddHours(2),
        signingCredentials: new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            SecurityAlgorithms.HmacSha256));

    return Results.Ok(new
    {
        accessToken = new JwtSecurityTokenHandler().WriteToken(token),
        expiresInSeconds = 7200,
        tenant = request.TenantId,
        request.Roles
    });
}).AllowAnonymous().WithTags("Auth");

app.MapPost("/api/v1/copilot/query", async (
    CopilotQueryRequest request,
    ClaimsPrincipal user,
    ICopilotQueryService service,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Question))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [nameof(request.Question)] = ["Question is required."]
        });
    }

    if (!TenantMatches(user, request.TenantId))
    {
        return Results.Forbid();
    }

    return Results.Ok(await service.QueryAsync(request, cancellationToken));
})
.RequireAuthorization()
.RequireRateLimiting("api")
.WithName("QueryCopilot")
.WithTags("Copilot")
.Produces<CopilotQueryResponse>(StatusCodes.Status200OK)
.ProducesValidationProblem();

app.MapPost("/api/v1/copilot/query/stream", async (
    CopilotQueryRequest request,
    ClaimsPrincipal user,
    ICopilotQueryService service,
    HttpResponse response,
    CancellationToken cancellationToken) =>
{
    if (!TenantMatches(user, request.TenantId))
    {
        response.StatusCode = StatusCodes.Status403Forbidden;
        return;
    }

    var result = await service.QueryAsync(request, cancellationToken);
    response.ContentType = "text/event-stream";
    foreach (var word in result.Answer.Split(' ', StringSplitOptions.RemoveEmptyEntries))
    {
        await response.WriteAsync($"data: {JsonSerializer.Serialize(new { delta = word + " " })}\n\n", cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
    }

    await response.WriteAsync($"event: complete\ndata: {JsonSerializer.Serialize(result)}\n\n", cancellationToken);
})
.RequireAuthorization()
.RequireRateLimiting("api")
.WithTags("Copilot");

app.MapPost("/api/v1/retrieval/hybrid-search", async (
    RetrievalQuery query,
    ClaimsPrincipal user,
    IHybridRetriever retriever,
    CancellationToken cancellationToken) =>
{
    if (!TenantMatches(user, query.TenantKey))
    {
        return Results.Forbid();
    }

    return Results.Ok(await retriever.SearchAsync(query, cancellationToken));
})
.RequireAuthorization()
.RequireRateLimiting("api")
.WithTags("Retrieval");

app.MapGet("/api/v1/retrieval/search", async (
    string tenantId,
    string query,
    int? limit,
    IDocumentSearchService searchService,
    CancellationToken cancellationToken) =>
{
    if (!Guid.TryParse(tenantId, out var parsedTenantId) || parsedTenantId == Guid.Empty)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [nameof(tenantId)] = ["A valid tenant ID is required."]
        });
    }

    if (string.IsNullOrWhiteSpace(query))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [nameof(query)] = ["Search query is required."]
        });
    }

    var requestedLimit = Math.Clamp(limit ?? 5, 1, 20);
    var results = await searchService.SearchAsync(parsedTenantId, query, requestedLimit, cancellationToken);
    return Results.Ok(new { query, results });
})
.RequireAuthorization()
.RequireRateLimiting("api")
.WithName("SearchDocuments")
.WithTags("Retrieval")
.Produces(StatusCodes.Status200OK)
.ProducesValidationProblem();

app.MapPost("/api/v1/documents/ingest", async (
    IngestDocumentCommand command,
    ClaimsPrincipal user,
    IIngestionEventPublisher publisher,
    CancellationToken cancellationToken) =>
{
    if (!TenantMatches(user, command.TenantKey))
    {
        return Results.Forbid();
    }

    await publisher.PublishAsync(command, cancellationToken);
    return Results.Accepted(value: new
    {
        command.EventId,
        status = "accepted",
        mode = "asynchronous-when-kafka-enabled"
    });
})
.RequireAuthorization()
.RequireRateLimiting("api")
.WithTags("Documents");

app.MapGet("/api/v1/tools", (IToolRegistry registry) =>
    Results.Ok(registry.All.Select(x => new { x.Name, x.Description, x.IsConsequential })))
    .RequireAuthorization()
    .WithTags("Tools");

app.MapPost("/api/v1/tools/{toolName}/execute", async (
    string toolName,
    ToolExecutionRequest request,
    ClaimsPrincipal user,
    IToolRegistry registry,
    CancellationToken cancellationToken) =>
{
    var tool = registry.Find(toolName);
    if (tool is null)
    {
        return Results.NotFound(new { error = "tool_not_found" });
    }

    var context = new ToolContext(
        user.FindFirstValue("tenant") ?? string.Empty,
        user.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? "unknown",
        user.FindAll(ClaimTypes.Role).Select(x => x.Value).ToHashSet(StringComparer.OrdinalIgnoreCase));
    return Results.Ok(await tool.ExecuteAsync(request.Arguments, context, cancellationToken));
})
.RequireAuthorization()
.RequireRateLimiting("api")
.WithTags("Tools");

app.MapPost("/api/v1/approvals/{approvalId:guid}/decision", async (
    Guid approvalId,
    ApprovalDecisionRequest request,
    ClaimsPrincipal user,
    IApprovalService approvals,
    CancellationToken cancellationToken) =>
{
    if (!user.IsInRole("operator"))
    {
        return Results.Forbid();
    }

    var approved = await approvals.DecideAsync(
        approvalId,
        request.Approve,
        user.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? "unknown",
        cancellationToken);
    return Results.Ok(new { approvalId, approved, simulatedActionExecuted = approved });
})
.RequireAuthorization()
.RequireRateLimiting("api")
.WithTags("Approvals");

app.MapPost("/api/v1/workflows/run", async (
    CopilotQueryRequest request,
    ClaimsPrincipal user,
    ISupportWorkflowService workflow,
    CancellationToken cancellationToken) =>
{
    if (!TenantMatches(user, request.TenantId))
    {
        return Results.Forbid();
    }

    var context = new WorkflowRequest(
        request.Question,
        request.TenantId,
        user.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? "unknown",
        user.FindAll(ClaimTypes.Role).Select(x => x.Value).ToHashSet(StringComparer.OrdinalIgnoreCase));
    return Results.Ok(await workflow.RunAsync(context, cancellationToken));
})
.RequireAuthorization()
.RequireRateLimiting("api")
.WithTags("Workflow");

app.MapGet("/api/v1/observability/dashboard", (RequestTelemetry telemetryStore) =>
    Results.Ok(telemetryStore.Snapshot()))
    .RequireAuthorization()
    .WithTags("Observability");

app.Run();

static bool TenantMatches(ClaimsPrincipal user, string requestedTenant) =>
    string.Equals(user.FindFirstValue("tenant"), requestedTenant, StringComparison.OrdinalIgnoreCase);

public sealed record DemoTokenRequest(string UserId = "demo-user", string TenantId = "demo-tenant", string[]? RequestedRoles = null)
{
    public string[] Roles => RequestedRoles is { Length: > 0 } ? RequestedRoles : ["support", "operator"];
}

public sealed record ToolExecutionRequest(JsonElement Arguments);
public sealed record ApprovalDecisionRequest(bool Approve);
public partial class Program;
