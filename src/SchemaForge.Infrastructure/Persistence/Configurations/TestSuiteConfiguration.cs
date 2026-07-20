using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchemaForge.Domain.Identity;
using SchemaForge.Domain.Organizations;
using SchemaForge.Domain.Schemas;
using SchemaForge.Domain.Testing;
using SchemaForge.Infrastructure.Persistence.Serialization;

namespace SchemaForge.Infrastructure.Persistence.Configurations;

public sealed class TestSuiteConfiguration : IEntityTypeConfiguration<TestSuite>
{
    public void Configure(EntityTypeBuilder<TestSuite> builder)
    {
        builder.ToTable("test_suites");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");

        builder.Property(s => s.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.HasOne<Organization>().WithMany().HasForeignKey(s => s.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(s => s.SchemaDefinitionId).HasColumnName("schema_definition_id").IsRequired();
        builder.HasOne<SchemaDefinition>().WithMany().HasForeignKey(s => s.SchemaDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(s => s.Name).HasColumnName("name").IsRequired();
        builder.HasIndex(s => new { s.SchemaDefinitionId, s.Name }).IsUnique();

        builder.Property(s => s.Description).HasColumnName("description");

        builder.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(s => s.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at");
        builder.Property(s => s.UpdatedByUserId).HasColumnName("updated_by_user_id");

        builder.HasOne<User>().WithMany().HasForeignKey(s => s.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(s => s.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // TestCase is a bounded child collection (Step 3 §3), same normalized-child-table
        // pattern as Team/TeamMembership - not JSONB, since unlike a SchemaNode tree these are a
        // flat, independently-addressable (add/update/remove by id) list, not a recursively
        // nested structure always loaded/saved as one blob.
        var expectationComparer = new ValueComparer<TestExpectation>(
            (a, b) => TestExpectationJsonConverter.Serialize(a!) == TestExpectationJsonConverter.Serialize(b!),
            v => TestExpectationJsonConverter.Serialize(v).GetHashCode(),
            v => TestExpectationJsonConverter.Deserialize(TestExpectationJsonConverter.Serialize(v)));

        builder.OwnsMany(s => s.Cases, caseBuilder =>
        {
            caseBuilder.ToTable("test_cases");

            caseBuilder.WithOwner().HasForeignKey("test_suite_id");
            caseBuilder.HasKey(c => c.Id);

            caseBuilder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();
            caseBuilder.Property("test_suite_id").HasColumnName("test_suite_id");
            caseBuilder.Property(c => c.Name).HasColumnName("name").IsRequired();
            caseBuilder.Property(c => c.InputJson).HasColumnName("input_json").HasColumnType("jsonb").IsRequired();

            caseBuilder.Property(c => c.Expectation)
                .HasConversion(v => TestExpectationJsonConverter.Serialize(v), v => TestExpectationJsonConverter.Deserialize(v))
                .HasColumnName("expectation")
                .HasColumnType("jsonb")
                .IsRequired()
                .Metadata.SetValueComparer(expectationComparer);

            caseBuilder.HasIndex("test_suite_id", nameof(TestCase.Name)).IsUnique();
        });
        builder.Navigation(s => s.Cases).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(s => s.DomainEvents);
    }
}
