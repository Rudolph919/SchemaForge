using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchemaForge.Domain.Identity;
using SchemaForge.Domain.Organizations;

namespace SchemaForge.Infrastructure.Persistence.Configurations;

// Structural mapping only - the tenant-isolation query filter needs the runtime ITenantContext
// and is applied directly in SchemaForgeDbContext.OnModelCreating instead (see the comment there).
public sealed class OrganizationMembershipConfiguration : IEntityTypeConfiguration<OrganizationMembership>
{
    public void Configure(EntityTypeBuilder<OrganizationMembership> builder)
    {
        builder.ToTable("organization_memberships", t =>
        {
            t.HasCheckConstraint("ck_org_memberships_role", "role IN ('Owner', 'Admin', 'Member')");
            t.HasCheckConstraint(
                "ck_org_memberships_status", "status IN ('Invited', 'Active', 'Revoked')");
        });

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id");

        builder.Property(m => m.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.HasOne<Organization>().WithMany().HasForeignKey(m => m.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(m => m.UserId).HasColumnName("user_id").IsRequired();
        builder.HasOne<User>().WithMany().HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => new { m.OrganizationId, m.UserId }).IsUnique();

        builder.Property(m => m.Role).HasConversion<string>().HasColumnName("role").IsRequired();
        builder.Property(m => m.Status).HasConversion<string>().HasColumnName("status").IsRequired();

        builder.Property(m => m.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(m => m.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(m => m.UpdatedAt).HasColumnName("updated_at");
        builder.Property(m => m.UpdatedByUserId).HasColumnName("updated_by_user_id");

        builder.HasOne<User>().WithMany().HasForeignKey(m => m.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(m => m.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(m => m.DomainEvents);
    }
}
