using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchemaForge.Domain.Identity;
using SchemaForge.Domain.Organizations;
using SchemaForge.Domain.Schemas;
using SchemaForge.Domain.Workspaces;

namespace SchemaForge.Infrastructure.Persistence.Configurations;

public sealed class SchemaDefinitionConfiguration : IEntityTypeConfiguration<SchemaDefinition>
{
    public void Configure(EntityTypeBuilder<SchemaDefinition> builder)
    {
        builder.ToTable("schema_definitions");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id");

        builder.Property(d => d.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.HasOne<Organization>().WithMany().HasForeignKey(d => d.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(d => d.ProjectId).HasColumnName("project_id").IsRequired();
        builder.HasOne<Project>().WithMany().HasForeignKey(d => d.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(d => d.Name).HasColumnName("name").IsRequired();
        builder.HasIndex(d => new { d.ProjectId, d.Name }).IsUnique();

        builder.Property(d => d.Description).HasColumnName("description");

        builder.Property(d => d.Tags).HasColumnName("tags").HasColumnType("text[]")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(d => d.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(d => d.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(d => d.UpdatedAt).HasColumnName("updated_at");
        builder.Property(d => d.UpdatedByUserId).HasColumnName("updated_by_user_id");

        builder.HasOne<User>().WithMany().HasForeignKey(d => d.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(d => d.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Backed by Postgres's built-in xmin system column (Step 6 §1.5's optimistic
        // concurrency) - a real mapped property, not a shadow one, so Application-layer
        // query handlers can read d.RowVersion directly without an EF Core reference.
        builder.Property(d => d.RowVersion).HasColumnName("xmin").IsRowVersion();

        builder.Ignore(d => d.DomainEvents);
    }
}
