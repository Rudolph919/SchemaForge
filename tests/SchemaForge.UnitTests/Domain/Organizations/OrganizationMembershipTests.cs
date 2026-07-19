using FluentAssertions;
using SchemaForge.Domain.Organizations;

namespace SchemaForge.UnitTests.Domain.Organizations;

public class OrganizationMembershipTests
{
    [Fact]
    public void CreateOwner_produces_an_active_owner_membership()
    {
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var membership = OrganizationMembership.CreateOwner(organizationId, userId);

        membership.OrganizationId.Should().Be(organizationId);
        membership.UserId.Should().Be(userId);
        membership.Role.Should().Be(OrganizationRole.Owner);
        membership.Status.Should().Be(MembershipStatus.Active);
    }

    [Fact]
    public void Invite_produces_an_invited_membership_and_raises_an_event()
    {
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var membership = OrganizationMembership.Invite(organizationId, userId, OrganizationRole.Member);

        membership.Status.Should().Be(MembershipStatus.Invited);
        membership.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<OrganizationMembershipInvited>();
    }

    [Fact]
    public void Accept_transitions_an_invited_membership_to_active()
    {
        var membership = OrganizationMembership.Invite(Guid.NewGuid(), Guid.NewGuid(), OrganizationRole.Member);

        var result = membership.Accept();

        result.IsSuccess.Should().BeTrue();
        membership.Status.Should().Be(MembershipStatus.Active);
        membership.DomainEvents.Should().Contain(e => e is OrganizationMembershipAccepted);
    }

    [Fact]
    public void Accept_fails_for_a_membership_that_is_not_invited()
    {
        var membership = OrganizationMembership.CreateOwner(Guid.NewGuid(), Guid.NewGuid());

        var result = membership.Accept();

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ChangeRole_updates_the_role_and_raises_an_event()
    {
        var membership = OrganizationMembership.CreateOwner(Guid.NewGuid(), Guid.NewGuid());

        var result = membership.ChangeRole(OrganizationRole.Admin);

        result.IsSuccess.Should().BeTrue();
        membership.Role.Should().Be(OrganizationRole.Admin);
        membership.DomainEvents.Should().Contain(e => e is OrganizationMembershipRoleChanged);
    }

    [Fact]
    public void ChangeRole_to_the_same_role_succeeds_without_raising_an_event()
    {
        var membership = OrganizationMembership.CreateOwner(Guid.NewGuid(), Guid.NewGuid());

        var result = membership.ChangeRole(OrganizationRole.Owner);

        result.IsSuccess.Should().BeTrue();
        membership.DomainEvents.Should().NotContain(e => e is OrganizationMembershipRoleChanged);
    }

    [Fact]
    public void ChangeRole_fails_for_a_revoked_membership()
    {
        var membership = OrganizationMembership.CreateOwner(Guid.NewGuid(), Guid.NewGuid());
        membership.Revoke();

        var result = membership.ChangeRole(OrganizationRole.Admin);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Revoke_transitions_to_revoked_and_raises_an_event()
    {
        var membership = OrganizationMembership.CreateOwner(Guid.NewGuid(), Guid.NewGuid());

        var result = membership.Revoke();

        result.IsSuccess.Should().BeTrue();
        membership.Status.Should().Be(MembershipStatus.Revoked);
        membership.DomainEvents.Should().Contain(e => e is OrganizationMembershipRevoked);
    }

    [Fact]
    public void Revoke_fails_for_an_already_revoked_membership()
    {
        var membership = OrganizationMembership.CreateOwner(Guid.NewGuid(), Guid.NewGuid());
        membership.Revoke();

        var result = membership.Revoke();

        result.IsFailure.Should().BeTrue();
    }
}
