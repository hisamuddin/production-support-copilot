using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionSupportCopilot.Domain.Tenants;

namespace ProductionSupportCopilot.Infrastructure.Persistence.Configurations;

internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");
        builder.HasKey(tenant => tenant.Id);
        builder.Property(tenant => tenant.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(tenant => tenant.Name).IsUnique();
    }
}
