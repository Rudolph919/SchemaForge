using FluentValidation;

namespace SchemaForge.Application.Components.Commands.AddComponentLocalDefinition;

public sealed class AddComponentLocalDefinitionValidator : AbstractValidator<AddComponentLocalDefinitionCommand>
{
    public AddComponentLocalDefinitionValidator()
    {
        RuleFor(x => x.ComponentVersionId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
    }
}
