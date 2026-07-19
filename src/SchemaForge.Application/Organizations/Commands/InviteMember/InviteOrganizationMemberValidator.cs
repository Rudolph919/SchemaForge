using FluentValidation;

namespace SchemaForge.Application.Organizations.Commands.InviteMember;

public sealed class InviteOrganizationMemberValidator : AbstractValidator<InviteOrganizationMemberCommand>
{
    public InviteOrganizationMemberValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Role).IsInEnum();
    }
}
