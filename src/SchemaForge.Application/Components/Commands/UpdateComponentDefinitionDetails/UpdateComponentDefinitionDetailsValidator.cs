using FluentValidation;

namespace SchemaForge.Application.Components.Commands.UpdateComponentDefinitionDetails;

public sealed class UpdateComponentDefinitionDetailsValidator : AbstractValidator<UpdateComponentDefinitionDetailsCommand>
{
    public UpdateComponentDefinitionDetailsValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}
