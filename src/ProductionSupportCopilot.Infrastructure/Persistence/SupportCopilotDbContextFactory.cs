using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProductionSupportCopilot.Infrastructure.Persistence;

public sealed class SupportCopilotDbContextFactory : IDesignTimeDbContextFactory<SupportCopilotDbContext>
{
    public SupportCopilotDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("SUPPORT_COPILOT_DB")
            ?? "Host=localhost;Port=5432;Database=support_copilot;Username=support_copilot;Password=local_development_only";

        var options = new DbContextOptionsBuilder<SupportCopilotDbContext>()
            .UseNpgsql(connectionString, postgres => postgres.UseVector())
            .Options;

        return new SupportCopilotDbContext(options);
    }
}
