using FluentValidation;

namespace SchemaForge.Application.Organizations.Commands.UpdateTeamDetails;

public sealed class UpdateTeamDetailsValidator : AbstractValidator<UpdateTeamDetailsCommand>
{
    public UpdateTeamDetailsValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}
