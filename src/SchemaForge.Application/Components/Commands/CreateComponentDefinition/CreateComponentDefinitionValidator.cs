using FluentValidation;

namespace SchemaForge.Application.Components.Commands.CreateComponentDefinition;

public sealed class CreateComponentDefinitionValidator : AbstractValidator<CreateComponentDefinitionCommand>
{
    public CreateComponentDefinitionValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}
