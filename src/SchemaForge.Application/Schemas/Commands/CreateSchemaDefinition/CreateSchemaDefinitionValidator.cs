using FluentValidation;

namespace SchemaForge.Application.Schemas.Commands.CreateSchemaDefinition;

public sealed class CreateSchemaDefinitionValidator : AbstractValidator<CreateSchemaDefinitionCommand>
{
    public CreateSchemaDefinitionValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}
