using ProductionSupportCopilot.Domain.Common;

namespace ProductionSupportCopilot.Domain.Workflow;

public enum ApprovalStatus { Pending, Approved, Rejected, Expired }

public sealed class ApprovalRequest : Entity
{
    private ApprovalRequest() { }
    public ApprovalRequest(Guid tenantId, string action, string argumentsJson, string requestedBy)
    {
        TenantId = tenantId; Action = action; ArgumentsJson = argumentsJson; RequestedBy = requestedBy;
        ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(15);
    }

    public Guid TenantId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string ArgumentsJson { get; private set; } = "{}";
    public string RequestedBy { get; private set; } = string.Empty;
    public string? DecidedBy { get; private set; }
    public ApprovalStatus Status { get; private set; } = ApprovalStatus.Pending;
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? DecidedAtUtc { get; private set; }

    public void Decide(bool approve, string decidedBy)
    {
        if (Status != ApprovalStatus.Pending) throw new InvalidOperationException("Approval has already been decided.");
        if (DateTimeOffset.UtcNow > ExpiresAtUtc) { Status = ApprovalStatus.Expired; throw new InvalidOperationException("Approval has expired."); }
        Status = approve ? ApprovalStatus.Approved : ApprovalStatus.Rejected;
        DecidedBy = decidedBy; DecidedAtUtc = DateTimeOffset.UtcNow; MarkUpdated();
    }
}
