using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchemaForge.Domain.Components;
using SchemaForge.Domain.Identity;
using SchemaForge.Domain.Organizations;

namespace SchemaForge.Infrastructure.Persistence.Configurations;

public sealed class ComponentDefinitionConfiguration : IEntityTypeConfiguration<ComponentDefinition>
{
    public void Configure(EntityTypeBuilder<ComponentDefinition> builder)
    {
        builder.ToTable("component_definitions");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id");

        builder.Property(d => d.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.HasOne<Organization>().WithMany().HasForeignKey(d => d.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Organization-scoped, not Project-scoped (Step 3 §2's "shared across every schema in
        // the org," unlike SchemaDefinition's per-Project name uniqueness).
        builder.Property(d => d.Name).HasColumnName("name").IsRequired();
        builder.HasIndex(d => new { d.OrganizationId, d.Name }).IsUnique();

        builder.Property(d => d.Description).HasColumnName("description");

        builder.Property(d => d.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(d => d.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(d => d.UpdatedAt).HasColumnName("updated_at");
        builder.Property(d => d.UpdatedByUserId).HasColumnName("updated_by_user_id");

        builder.HasOne<User>().WithMany().HasForeignKey(d => d.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(d => d.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(d => d.DomainEvents);
    }
}
