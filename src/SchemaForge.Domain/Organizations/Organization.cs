using SchemaForge.SharedKernel;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.Domain.Organizations;

// AuditableEntity, not TenantOwnedAggregateRoot: an Organization IS the tenant boundary, so
// "filter by OrganizationId" would be checking its own Id against itself - there's nothing to
// filter. Step 5's own schema agrees: the organizations table has no organization_id column.
public sealed class Organization : AuditableEntity<Guid>
{
    public string Name { get; private set; } = null!;

    public Slug Slug { get; private set; } = null!;

    public PlanTier PlanTier { get; private set; }

    public OrganizationStatus Status { get; private set; }

    private Organization() { } // EF Core materialization

    private Organization(Guid id, string name, Slug slug) : base(id)
    {
        Name = name;
        Slug = slug;
        PlanTier = PlanTier.Free;
        Status = OrganizationStatus.Active;
    }

    public static Organization Create(string name, Slug slug)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Organization name is required.", nameof(name));

        var organization = new Organization(Guid.NewGuid(), name, slug);
        organization.RaiseDomainEvent(new OrganizationCreated(organization.Id, name));

        return organization;
    }
}
