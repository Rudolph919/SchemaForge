using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchemaForge.Domain.Identity;
using SchemaForge.Domain.Organizations;
using SchemaForge.Domain.Schemas;
using SchemaForge.Domain.Testing;
using SchemaForge.Infrastructure.Persistence.Serialization;

namespace SchemaForge.Infrastructure.Persistence.Configurations;

public sealed class TestRunConfiguration : IEntityTypeConfiguration<TestRun>
{
    public void Configure(EntityTypeBuilder<TestRun> builder)
    {
        builder.ToTable("test_runs");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");

        builder.Property(r => r.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.HasOne<Organization>().WithMany().HasForeignKey(r => r.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.TestSuiteId).HasColumnName("test_suite_id").IsRequired();
        builder.HasOne<TestSuite>().WithMany().HasForeignKey(r => r.TestSuiteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.SchemaVersionId).HasColumnName("schema_version_id").IsRequired();
        builder.HasOne<SchemaVersion>().WithMany().HasForeignKey(r => r.SchemaVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.Status).HasConversion<string>().HasColumnName("status").IsRequired();
        builder.Property(r => r.ExecutedAt).HasColumnName("executed_at").IsRequired();
        builder.Property(r => r.ExecutedByUserId).HasColumnName("executed_by_user_id").IsRequired();
        builder.HasOne<User>().WithMany().HasForeignKey(r => r.ExecutedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        var resultsComparer = new ValueComparer<IReadOnlyList<TestCaseResult>>(
            (a, b) => TestCaseResultJsonConverter.Serialize(a!) == TestCaseResultJsonConverter.Serialize(b!),
            v => TestCaseResultJsonConverter.Serialize(v).GetHashCode(),
            v => TestCaseResultJsonConverter.Deserialize(TestCaseResultJsonConverter.Serialize(v)));

        builder.Property(r => r.Results)
            .HasConversion(v => TestCaseResultJsonConverter.Serialize(v), v => TestCaseResultJsonConverter.Deserialize(v))
            .HasColumnName("results")
            .HasColumnType("jsonb")
            .IsRequired()
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .Metadata.SetValueComparer(resultsComparer);

        builder.HasIndex(r => new { r.TestSuiteId, r.ExecutedAt });

        // TestRun is immutable (ExecutedAt/ExecutedByUserId already capture "when created and by
        // whom") - the illustrative Step 4 class sketch omits AuditableEntity for it precisely
        // because of that, but the codebase's actual hierarchy bundles those columns into every
        // TenantOwnedAggregateRoot unconditionally (Step 4 can't be expressed as multiple
        // inheritance in C#), so they're mapped here the same as everywhere else - UpdatedAt/
        // UpdatedByUserId just always stay null for this entity, same as ValidationRun.
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(r => r.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");
        builder.Property(r => r.UpdatedByUserId).HasColumnName("updated_by_user_id");
        builder.HasOne<User>().WithMany().HasForeignKey(r => r.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(r => r.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(r => r.DomainEvents);
    }
}
