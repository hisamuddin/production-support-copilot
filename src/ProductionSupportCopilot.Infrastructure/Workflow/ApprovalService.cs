using Microsoft.EntityFrameworkCore;
using ProductionSupportCopilot.Application.Workflow;
using ProductionSupportCopilot.Domain.Workflow;
using ProductionSupportCopilot.Infrastructure.Persistence;

namespace ProductionSupportCopilot.Infrastructure.Workflow;

public sealed class ApprovalService(SupportCopilotDbContext db) : IApprovalService
{
    public async Task<Guid> RequestAsync(string tenantKey, string action, string argumentsJson, string requestedBy, CancellationToken cancellationToken = default)
    {
        var tenantId = await db.Tenants.Where(x => x.Key == tenantKey).Select(x => x.Id).SingleAsync(cancellationToken);
        var approval = new ApprovalRequest(tenantId, action, argumentsJson, requestedBy);
        db.ApprovalRequests.Add(approval);
        await db.SaveChangesAsync(cancellationToken);
        return approval.Id;
    }

    public async Task<bool> DecideAsync(Guid approvalId, bool approve, string decidedBy, CancellationToken cancellationToken = default)
    {
        var approval = await db.ApprovalRequests.SingleOrDefaultAsync(x => x.Id == approvalId, cancellationToken) ?? throw new KeyNotFoundException("Approval not found.");
        approval.Decide(approve, decidedBy);
        await db.SaveChangesAsync(cancellationToken);
        return approval.Status == ApprovalStatus.Approved;
    }
}
