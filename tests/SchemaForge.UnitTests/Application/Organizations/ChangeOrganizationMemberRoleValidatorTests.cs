using FluentAssertions;
using SchemaForge.Application.Organizations.Commands.ChangeMemberRole;
using SchemaForge.Domain.Organizations;

namespace SchemaForge.UnitTests.Application.Organizations;

public class ChangeOrganizationMemberRoleValidatorTests
{
    private readonly ChangeOrganizationMemberRoleValidator _validator = new();

    [Fact]
    public void Valid_command_passes()
    {
        var result = _validator.Validate(new ChangeOrganizationMemberRoleCommand(Guid.NewGuid(), OrganizationRole.Admin));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Invalid_role_fails()
    {
        var result = _validator.Validate(new ChangeOrganizationMemberRoleCommand(Guid.NewGuid(), (OrganizationRole)999));

        result.IsValid.Should().BeFalse();
    }
}
