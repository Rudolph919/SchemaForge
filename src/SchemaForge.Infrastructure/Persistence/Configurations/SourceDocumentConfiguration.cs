using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchemaForge.Domain.Identity;
using SchemaForge.Domain.Organizations;
using SchemaForge.Domain.Workspaces;

namespace SchemaForge.Infrastructure.Persistence.Configurations;

public sealed class SourceDocumentConfiguration : IEntityTypeConfiguration<SourceDocument>
{
    public void Configure(EntityTypeBuilder<SourceDocument> builder)
    {
        builder.ToTable("source_documents");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id");

        builder.Property(d => d.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.HasOne<Organization>().WithMany().HasForeignKey(d => d.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(d => d.ProjectId).HasColumnName("project_id").IsRequired();
        builder.HasOne<Project>().WithMany().HasForeignKey(d => d.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(d => d.FileName).HasColumnName("file_name").IsRequired();
        builder.Property(d => d.StorageKey).HasColumnName("storage_key").IsRequired();
        builder.Property(d => d.ContentType).HasColumnName("content_type").IsRequired();
        builder.Property(d => d.SizeBytes).HasColumnName("size_bytes").IsRequired();

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
