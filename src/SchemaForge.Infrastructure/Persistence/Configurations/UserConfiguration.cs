using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchemaForge.Domain.Identity;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id");

        builder.Property(u => u.Email)
            .HasConversion(email => email.Value, value => EmailAddress.Create(value))
            .HasColumnName("email")
            .HasColumnType("citext")
            .IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PasswordHash).HasColumnName("password_hash").IsRequired();
        builder.Property(u => u.DisplayName).HasColumnName("display_name").IsRequired();
        builder.Property(u => u.EmailVerified).HasColumnName("email_verified").IsRequired();

        builder.Property(u => u.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(u => u.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at");
        builder.Property(u => u.UpdatedByUserId).HasColumnName("updated_by_user_id");

        // Self-referencing: a User can (rarely) be created/updated by another User (e.g. an
        // admin-driven action later); null covers self-registration, the only path today.
        builder.HasOne<User>().WithMany().HasForeignKey(u => u.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(u => u.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(u => u.DomainEvents);
    }
}
