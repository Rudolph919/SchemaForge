using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchemaForge.Domain.Identity;
using SchemaForge.Domain.Organizations;
using SchemaForge.Domain.Workspaces;

namespace SchemaForge.Infrastructure.Persistence.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects", t =>
            t.HasCheckConstraint("ck_projects_status", "status IN ('Active', 'Archived')"));

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");

        builder.Property(p => p.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.HasOne<Organization>().WithMany().HasForeignKey(p => p.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.Name).HasColumnName("name").IsRequired();
        builder.HasIndex(p => new { p.OrganizationId, p.Name }).IsUnique();

        builder.Property(p => p.Description).HasColumnName("description");
        builder.Property(p => p.Status).HasConversion<string>().HasColumnName("status").IsRequired();

        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(p => p.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
        builder.Property(p => p.UpdatedByUserId).HasColumnName("updated_by_user_id");

        builder.HasOne<User>().WithMany().HasForeignKey(p => p.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(p => p.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Backed by Postgres's built-in xmin system column (Step 6 §1.5's optimistic
        // concurrency) - a real mapped property, not a shadow one, so Application-layer
        // query handlers can read p.RowVersion directly without an EF Core reference.
        builder.Property(p => p.RowVersion).HasColumnName("xmin").IsRowVersion();

        builder.Ignore(p => p.DomainEvents);
    }
}
