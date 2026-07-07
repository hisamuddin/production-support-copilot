namespace ProductionSupportCopilot.Application.Copilot;

public sealed record CopilotQueryRequest(string Question, string TenantId = "demo-tenant");
