using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Organizations;

public sealed class OrganizationMembership : TenantOwnedAggregateRoot<Guid>
{
    public Guid UserId { get; private set; }

    public OrganizationRole Role { get; private set; }

    public MembershipStatus Status { get; private set; }

    private OrganizationMembership() { } // EF Core materialization

    private OrganizationMembership(
        Guid id, Guid organizationId, Guid userId, OrganizationRole role, MembershipStatus status)
        : base(id, organizationId)
    {
        UserId = userId;
        Role = role;
        Status = status;
    }

    // Registration bootstrap only: the very first membership of a brand-new Organization, so it
    // starts Active rather than Invited - there's no one else yet to have invited them, and
    // nothing to accept.
    public static OrganizationMembership CreateOwner(Guid organizationId, Guid userId) =>
        new(Guid.NewGuid(), organizationId, userId, OrganizationRole.Owner, MembershipStatus.Active);

    // The general path: inviting an existing user (found by email at the Application layer -
    // Domain doesn't know about email, only User ids). Starts Invited; the invitee accepts it
    // themselves via Accept().
    public static OrganizationMembership Invite(Guid organizationId, Guid userId, OrganizationRole role)
    {
        var membership = new OrganizationMembership(
            Guid.NewGuid(), organizationId, userId, role, MembershipStatus.Invited);

        membership.RaiseDomainEvent(new OrganizationMembershipInvited(organizationId, userId, role));

        return membership;
    }

    public Result Accept()
    {
        if (Status != MembershipStatus.Invited)
        {
            return Result.Failure(Error.Validation(
                "OrganizationMembership.NotInvited", "This membership is not a pending invitation."));
        }

        Status = MembershipStatus.Active;
        RaiseDomainEvent(new OrganizationMembershipAccepted(OrganizationId, UserId));

        return Result.Success();
    }

    public Result ChangeRole(OrganizationRole newRole)
    {
        if (Status == MembershipStatus.Revoked)
        {
            return Result.Failure(Error.Validation(
                "OrganizationMembership.Revoked", "Cannot change the role of a revoked membership."));
        }

        if (Role == newRole)
        {
            return Result.Success();
        }

        var oldRole = Role;
        Role = newRole;
        RaiseDomainEvent(new OrganizationMembershipRoleChanged(OrganizationId, UserId, oldRole, newRole));

        return Result.Success();
    }

    public Result Revoke()
    {
        if (Status == MembershipStatus.Revoked)
        {
            return Result.Failure(Error.Validation(
                "OrganizationMembership.AlreadyRevoked", "This membership is already revoked."));
        }

        Status = MembershipStatus.Revoked;
        RaiseDomainEvent(new OrganizationMembershipRevoked(OrganizationId, UserId));

        return Result.Success();
    }
}
