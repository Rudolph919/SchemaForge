using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchemaForge.Domain.Components;
using SchemaForge.Domain.Identity;
using SchemaForge.Domain.Organizations;
using SchemaForge.Domain.Schemas;
using SchemaForge.Infrastructure.Persistence.Serialization;

namespace SchemaForge.Infrastructure.Persistence.Configurations;

// Identical shape to SchemaVersionConfiguration (Step 4 §5's "no new concepts") - same jsonb
// value-converter approach for RootNode/LocalDefinitions, reusing SchemaNodeJsonConverter
// directly since it already operates on the shared SchemaNode/LocalDefinition types rather than
// on SchemaVersion itself. Only the parent FK (ComponentDefinitionId) and index name differ.
public sealed class ComponentVersionConfiguration : IEntityTypeConfiguration<ComponentVersion>
{
    public void Configure(EntityTypeBuilder<ComponentVersion> builder)
    {
        builder.ToTable("component_versions");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasColumnName("id");

        builder.Property(v => v.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.HasOne<Organization>().WithMany().HasForeignKey(v => v.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(v => v.ComponentDefinitionId).HasColumnName("component_definition_id").IsRequired();
        builder.HasOne<ComponentDefinition>().WithMany().HasForeignKey(v => v.ComponentDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(v => v.VersionNumber, semVer =>
        {
            semVer.Property(s => s.Major).HasColumnName("version_major").IsRequired();
            semVer.Property(s => s.Minor).HasColumnName("version_minor").IsRequired();
            semVer.Property(s => s.Patch).HasColumnName("version_patch").IsRequired();
        });
        builder.Navigation(v => v.VersionNumber).IsRequired();

        builder.Property(v => v.Status).HasConversion<string>().HasColumnName("status").IsRequired();
        builder.Property(v => v.ChangeSummary).HasColumnName("change_summary");
        builder.Property(v => v.PublishedAt).HasColumnName("published_at");

        var nodeComparer = new ValueComparer<SchemaNode>(
            (a, b) => SchemaNodeJsonConverter.SerializeNode(a!) == SchemaNodeJsonConverter.SerializeNode(b!),
            v => SchemaNodeJsonConverter.SerializeNode(v).GetHashCode(),
            v => SchemaNodeJsonConverter.DeserializeNode(SchemaNodeJsonConverter.SerializeNode(v)));

        builder.Property(v => v.RootNode)
            .HasConversion(v => SchemaNodeJsonConverter.SerializeNode(v), v => SchemaNodeJsonConverter.DeserializeNode(v))
            .HasColumnName("root_node")
            .HasColumnType("jsonb")
            .IsRequired()
            .Metadata.SetValueComparer(nodeComparer);

        var localDefinitionsComparer = new ValueComparer<IReadOnlyList<LocalDefinition>>(
            (a, b) => SchemaNodeJsonConverter.SerializeLocalDefinitions(a!) == SchemaNodeJsonConverter.SerializeLocalDefinitions(b!),
            v => SchemaNodeJsonConverter.SerializeLocalDefinitions(v).GetHashCode(),
            v => SchemaNodeJsonConverter.DeserializeLocalDefinitions(SchemaNodeJsonConverter.SerializeLocalDefinitions(v)));

        builder.Property(v => v.LocalDefinitions)
            .HasConversion(
                v => SchemaNodeJsonConverter.SerializeLocalDefinitions(v),
                v => SchemaNodeJsonConverter.DeserializeLocalDefinitions(v))
            .HasColumnName("local_definitions")
            .HasColumnType("jsonb")
            .IsRequired()
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .Metadata.SetValueComparer(localDefinitionsComparer);

        builder.Property(v => v.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(v => v.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(v => v.UpdatedAt).HasColumnName("updated_at");
        builder.Property(v => v.UpdatedByUserId).HasColumnName("updated_by_user_id");

        builder.HasOne<User>().WithMany().HasForeignKey(v => v.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(v => v.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // One Draft at a time per ComponentDefinition - same partial-unique-index pattern as
        // SchemaVersion (Step 3 §4).
        builder.HasIndex(v => v.ComponentDefinitionId)
            .HasDatabaseName("ux_component_versions_one_draft")
            .IsUnique()
            .HasFilter("status = 'Draft'");

        builder.HasIndex(v => v.ComponentDefinitionId);

        builder.Ignore(v => v.DomainEvents);
    }
}
