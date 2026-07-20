using FluentValidation;

namespace SchemaForge.Application.Components.Commands.CreateComponentVersion;

public sealed class CreateComponentVersionValidator : AbstractValidator<CreateComponentVersionCommand>
{
    public CreateComponentVersionValidator()
    {
        RuleFor(x => x.ComponentDefinitionId).NotEmpty();
        RuleFor(x => x.BumpKind).IsInEnum();
        RuleFor(x => x.ChangeSummary).MaximumLength(2000);
    }
}
