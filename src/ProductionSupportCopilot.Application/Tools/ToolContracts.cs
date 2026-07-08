using System.Text.Json;

namespace ProductionSupportCopilot.Application.Tools;

public sealed record ToolContext(string TenantKey, string UserId, IReadOnlySet<string> Roles);
public sealed record ToolResult(bool Success, string Summary, object Data, bool RequiresApproval = false, Guid? ApprovalId = null);

public interface ISupportTool
{
    string Name { get; }
    string Description { get; }
    bool IsConsequential { get; }
    Task<ToolResult> ExecuteAsync(JsonElement arguments, ToolContext context, CancellationToken cancellationToken = default);
}

public interface IToolRegistry
{
    IReadOnlyCollection<ISupportTool> All { get; }
    ISupportTool? Find(string name);
}

public sealed class ToolRegistry(IEnumerable<ISupportTool> tools) : IToolRegistry
{
    private readonly IReadOnlyDictionary<string, ISupportTool> _tools = tools.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
    public IReadOnlyCollection<ISupportTool> All => _tools.Values.ToList();
    public ISupportTool? Find(string name) => _tools.GetValueOrDefault(name);
}
