using ProductionSupportCopilot.Domain.Common;

namespace ProductionSupportCopilot.Domain.Knowledge;

public sealed class Tenant : Entity
{
    private Tenant() { }

    public Tenant(string key, string name)
    {
        Key = Require(key, nameof(key));
        Name = Require(name, nameof(name));
    }

    public string Key { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public ICollection<SupportDocument> Documents { get; private set; } = [];

    private static string Require(string value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value is required.", name) : value.Trim();
}
