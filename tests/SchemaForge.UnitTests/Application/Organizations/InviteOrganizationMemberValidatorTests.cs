using FluentAssertions;
using SchemaForge.Application.Organizations.Commands.InviteMember;
using SchemaForge.Domain.Organizations;

namespace SchemaForge.UnitTests.Application.Organizations;

public class InviteOrganizationMemberValidatorTests
{
    private readonly InviteOrganizationMemberValidator _validator = new();

    [Fact]
    public void Valid_command_passes()
    {
        var result = _validator.Validate(new InviteOrganizationMemberCommand("ada@example.com", OrganizationRole.Member));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Invalid_email_fails(string email)
    {
        var result = _validator.Validate(new InviteOrganizationMemberCommand(email, OrganizationRole.Member));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Invalid_role_fails()
    {
        var result = _validator.Validate(new InviteOrganizationMemberCommand("ada@example.com", (OrganizationRole)999));

        result.IsValid.Should().BeFalse();
    }
}
