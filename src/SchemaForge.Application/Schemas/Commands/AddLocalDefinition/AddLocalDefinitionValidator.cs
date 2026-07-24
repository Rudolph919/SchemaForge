using FluentValidation;

namespace SchemaForge.Application.Schemas.Commands.AddLocalDefinition;

public sealed class AddLocalDefinitionValidator : AbstractValidator<AddLocalDefinitionCommand>
{
    public AddLocalDefinitionValidator()
    {
        RuleFor(x => x.SchemaVersionId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
    }
}
