using FluentValidation;

namespace SchemaForge.Application.Organizations.Commands.ChangeMemberRole;

public sealed class ChangeOrganizationMemberRoleValidator : AbstractValidator<ChangeOrganizationMemberRoleCommand>
{
    public ChangeOrganizationMemberRoleValidator()
    {
        RuleFor(x => x.NewRole).IsInEnum();
    }
}
