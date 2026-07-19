using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Organizations;

// Minimal for the Phase 0 walking skeleton (registration only creates an initial Owner
// membership). Role changes, invites, and the last-Owner guard (Step 3 §4) land in Phase 1
// when Team/membership management is actually built.
public sealed class OrganizationMembership : TenantOwnedAggregateRoot<Guid>
{
    public Guid UserId { get; private set; }

    public OrganizationRole Role { get; private set; }

    public MembershipStatus Status { get; private set; }

    private OrganizationMembership() { } // EF Core materialization

    private OrganizationMembership(Guid id, Guid organizationId, Guid userId, OrganizationRole role)
        : base(id, organizationId)
    {
        UserId = userId;
        Role = role;
        Status = MembershipStatus.Active;
    }

    public static OrganizationMembership CreateOwner(Guid organizationId, Guid userId) =>
        new(Guid.NewGuid(), organizationId, userId, OrganizationRole.Owner);
}
