using FluentValidation;

namespace SchemaForge.Application.Identity.Commands.SwitchOrganization;

public sealed class SwitchOrganizationValidator : AbstractValidator<SwitchOrganizationCommand>
{
    public SwitchOrganizationValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
    }
}
