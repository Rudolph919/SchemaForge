using FluentValidation;

namespace SchemaForge.Application.Schemas.Commands.CreateSchemaVersion;

public sealed class CreateSchemaVersionValidator : AbstractValidator<CreateSchemaVersionCommand>
{
    public CreateSchemaVersionValidator()
    {
        RuleFor(x => x.SchemaDefinitionId).NotEmpty();
        RuleFor(x => x.BumpKind).IsInEnum();
        RuleFor(x => x.ChangeSummary).MaximumLength(2000);
    }
}
