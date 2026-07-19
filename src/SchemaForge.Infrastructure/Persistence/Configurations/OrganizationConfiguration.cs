using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchemaForge.Domain.Identity;
using SchemaForge.Domain.Organizations;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.Infrastructure.Persistence.Configurations;

public sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organizations", t =>
        {
            t.HasCheckConstraint("ck_organizations_plan_tier", "plan_tier IN ('Free', 'Pro', 'Enterprise')");
            t.HasCheckConstraint("ck_organizations_status", "status IN ('Active', 'Suspended')");
        });

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnName("id");

        builder.Property(o => o.Name).HasColumnName("name").IsRequired();

        builder.Property(o => o.Slug)
            .HasConversion(slug => slug.Value, value => Slug.Create(value))
            .HasColumnName("slug")
            .IsRequired();
        builder.HasIndex(o => o.Slug).IsUnique();

        builder.Property(o => o.PlanTier)
            .HasConversion<string>()
            .HasColumnName("plan_tier")
            .IsRequired();

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasColumnName("status")
            .IsRequired();

        builder.Property(o => o.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(o => o.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(o => o.UpdatedAt).HasColumnName("updated_at");
        builder.Property(o => o.UpdatedByUserId).HasColumnName("updated_by_user_id");

        builder.HasOne<User>().WithMany().HasForeignKey(o => o.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(o => o.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(o => o.DomainEvents);
    }
}
