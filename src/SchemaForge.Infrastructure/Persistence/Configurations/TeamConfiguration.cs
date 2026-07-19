using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchemaForge.Domain.Identity;
using SchemaForge.Domain.Organizations;

namespace SchemaForge.Infrastructure.Persistence.Configurations;

public sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("teams");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");

        builder.Property(t => t.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.HasOne<Organization>().WithMany().HasForeignKey(t => t.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(t => t.Name).HasColumnName("name").IsRequired();
        builder.HasIndex(t => new { t.OrganizationId, t.Name }).IsUnique();

        builder.Property(t => t.Description).HasColumnName("description");

        builder.Property(t => t.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(t => t.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at");
        builder.Property(t => t.UpdatedByUserId).HasColumnName("updated_by_user_id");

        builder.HasOne<User>().WithMany().HasForeignKey(t => t.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(t => t.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // TeamMembership is a bounded child collection (Step 3 Ground Rule 2), so a normalized
        // child table via a private backing field is the right shape here - unlike SchemaNode
        // trees later, which get JSONB specifically because they're unbounded-depth and always
        // loaded/saved as one atomic unit with their aggregate.
        builder.OwnsMany(t => t.Members, membershipBuilder =>
        {
            membershipBuilder.ToTable("team_memberships");

            membershipBuilder.WithOwner().HasForeignKey("team_id");
            membershipBuilder.HasKey(m => m.Id);

            membershipBuilder.Property(m => m.Id).HasColumnName("id").ValueGeneratedNever();
            membershipBuilder.Property("team_id").HasColumnName("team_id");
            membershipBuilder.Property(m => m.UserId).HasColumnName("user_id").IsRequired();
            membershipBuilder.Property(m => m.JoinedAt).HasColumnName("joined_at").IsRequired();

            membershipBuilder.HasIndex("team_id", nameof(TeamMembership.UserId)).IsUnique();
            membershipBuilder.HasOne<User>().WithMany().HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        builder.Navigation(t => t.Members).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(t => t.DomainEvents);
    }
}
