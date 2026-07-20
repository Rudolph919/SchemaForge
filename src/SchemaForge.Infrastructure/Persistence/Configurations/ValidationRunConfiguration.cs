using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchemaForge.Domain.Identity;
using SchemaForge.Domain.Organizations;
using SchemaForge.Domain.Schemas;
using SchemaForge.Domain.Validation;
using SchemaForge.Domain.Workspaces;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.Infrastructure.Persistence.Configurations;

public sealed class ValidationRunConfiguration : IEntityTypeConfiguration<ValidationRun>
{
    public void Configure(EntityTypeBuilder<ValidationRun> builder)
    {
        builder.ToTable("validation_runs");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");

        builder.Property(r => r.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.HasOne<Organization>().WithMany().HasForeignKey(r => r.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.ProjectId).HasColumnName("project_id").IsRequired();
        builder.HasOne<Project>().WithMany().HasForeignKey(r => r.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.SchemaVersionId).HasColumnName("schema_version_id").IsRequired();
        builder.HasOne<SchemaVersion>().WithMany().HasForeignKey(r => r.SchemaVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.InputPayloadHash).HasColumnName("input_payload_hash").HasMaxLength(64).IsRequired();
        builder.Property(r => r.Outcome).HasConversion<string>().HasColumnName("outcome").IsRequired();
        builder.Property(r => r.ExecutedAt).HasColumnName("executed_at").IsRequired();
        builder.Property(r => r.ExecutedByUserId).HasColumnName("executed_by_user_id").IsRequired();
        builder.HasOne<User>().WithMany().HasForeignKey(r => r.ExecutedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unlike SchemaNode, ValidationError isn't recursive - EF's native owned-JSON-column
        // mapping (unlike the hand-written converter SchemaVersion needs) is the right fit here.
        // Path is mapped as a converted scalar (string), not a nested OwnsOne: EF's constructor
        // binding for ValidationError's positional-record constructor can't bind a nested owned
        // navigation as a parameter ("no suitable constructor found," confirmed by actually
        // trying it) - a value converter keeps Path a plain scalar from EF's perspective, which
        // constructor binding handles the same way it already does for Status elsewhere.
        builder.OwnsMany(r => r.Errors, error =>
        {
            error.ToJson("errors");
            error.Property(e => e.Path).HasConversion(p => p.Value, v => JsonPath.Create(v));
        });
        builder.Navigation(r => r.Errors).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(r => r.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(r => r.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");
        builder.Property(r => r.UpdatedByUserId).HasColumnName("updated_by_user_id");
        builder.HasOne<User>().WithMany().HasForeignKey(r => r.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(r => r.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.SchemaVersionId, r.ExecutedAt });

        builder.Ignore(r => r.DomainEvents);
    }
}
