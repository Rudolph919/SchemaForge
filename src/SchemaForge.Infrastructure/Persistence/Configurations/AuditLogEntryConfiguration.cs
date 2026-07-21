using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchemaForge.Domain.Audit;
using SchemaForge.Domain.Identity;
using SchemaForge.Domain.Organizations;

namespace SchemaForge.Infrastructure.Persistence.Configurations;

public sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("audit_log_entries");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.ActorUserId).HasColumnName("actor_user_id").IsRequired();
        builder.HasOne<User>().WithMany().HasForeignKey(e => e.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.Action).HasColumnName("action").IsRequired();
        builder.Property(e => e.EntityType).HasColumnName("entity_type").IsRequired();
        builder.Property(e => e.EntityId).HasColumnName("entity_id").IsRequired();
        builder.Property(e => e.MetadataJson).HasColumnName("metadata").HasColumnType("jsonb");
        builder.Property(e => e.OccurredAt).HasColumnName("occurred_at").IsRequired();

        // Step 5 §6's two primary access patterns: a most-recent-first feed, and "show me the
        // history of this specific entity."
        builder.HasIndex(e => new { e.OrganizationId, e.OccurredAt });
        builder.HasIndex(e => new { e.OrganizationId, e.EntityType, e.EntityId, e.OccurredAt });

        // Step 7's index audit: GetAuditLogQuery's actorUserId filter (a third real access
        // pattern - "what has this person done") had no supporting index until now.
        builder.HasIndex(e => new { e.OrganizationId, e.ActorUserId, e.OccurredAt });

        // Every TenantOwnedAggregateRoot carries these (the hierarchy bundles AuditableEntity
        // in, Step 4 can't express it as multiple inheritance in C#) - mapped the same way as
        // TestRun/ValidationRun even though OccurredAt is the timestamp that actually matters
        // here; UpdatedAt/UpdatedByUserId just always stay null since nothing ever updates one.
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id");
        builder.HasOne<User>().WithMany().HasForeignKey(e => e.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(e => e.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(e => e.DomainEvents);
    }
}
