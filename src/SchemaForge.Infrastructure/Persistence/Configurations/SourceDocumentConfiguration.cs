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

        // Step 7's index audit: SourceDocumentRepository.GetAllForProjectAsync filters on
        // ProjectId alone. EF's own FK convention already creates an equivalent index
        // automatically (confirmed: `dotnet ef migrations add` generated no new CreateIndex for
        // this column), but declaring it explicitly documents that this index is load-bearing
        // for a real query, not just an FK-convention side effect that's safe to lose if the
        // relationship mapping ever changes shape.
        builder.HasIndex(d => d.ProjectId);

        builder.Ignore(d => d.DomainEvents);
    }
}
