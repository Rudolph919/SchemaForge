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
}
