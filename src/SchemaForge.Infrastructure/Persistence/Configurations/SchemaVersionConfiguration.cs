using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchemaForge.Domain.Identity;
using SchemaForge.Domain.Organizations;
using SchemaForge.Domain.Schemas;
using SchemaForge.Infrastructure.Persistence.Serialization;

namespace SchemaForge.Infrastructure.Persistence.Configurations;

public sealed class SchemaVersionConfiguration : IEntityTypeConfiguration<SchemaVersion>
{
    public void Configure(EntityTypeBuilder<SchemaVersion> builder)
    {
        builder.ToTable("schema_versions");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasColumnName("id");

        builder.Property(v => v.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.HasOne<Organization>().WithMany().HasForeignKey(v => v.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(v => v.SchemaDefinitionId).HasColumnName("schema_definition_id").IsRequired();
        builder.HasOne<SchemaDefinition>().WithMany().HasForeignKey(v => v.SchemaDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        // SemVer is a flat, non-recursive VO - EF's native owned-type mapping (unlike the
        // SchemaNode tree below) has no trouble with it.
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

        // The node tree: opaque jsonb via a hand-written value converter (Step 5 §2) - see
        // SchemaNodeJsonConverter's own comment for why this isn't EF's native ToJson() mapping.
        // A ValueComparer is required here specifically because the converted CLR type
        // (SchemaNode) is a mutable reference type: without one, EF's default reference-equality
        // change detection would never notice an in-place tree edit and would silently skip
        // writing it. Comparing/hashing via the serialized JSON is simplest and always correct,
        // even though it means re-serializing on every SaveChanges to check for a change - a
        // JSONB write per save is already the expected cost per Step 6 §3.
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

        // LocalDefinitions has no setter at all (a computed property over the private
        // _localDefinitions field) - field access mode is required or EF has nothing to write
        // the deserialized value into.
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

        // One Draft at a time per SchemaDefinition (Step 3 §4) - the app-layer check is a fast,
        // friendly pre-flight; this partial index is the actual concurrency-safe guarantee.
        builder.HasIndex(v => v.SchemaDefinitionId)
            .HasDatabaseName("ux_schema_versions_one_draft")
            .IsUnique()
            .HasFilter("status = 'Draft'");

        builder.HasIndex(v => v.SchemaDefinitionId);

        builder.Ignore(v => v.DomainEvents);
    }
}
