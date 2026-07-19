using FluentValidation;

namespace SchemaForge.Application.Identity.Commands.SwitchOrganization;

public sealed class SwitchOrganizationValidator : AbstractValidator<SwitchOrganizationQuery>
{
    public SwitchOrganizationValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
    }
}
