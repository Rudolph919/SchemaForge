using FluentValidation;

namespace SchemaForge.Application.Components.Commands.MoveComponentNode;

public sealed class MoveComponentNodeValidator : AbstractValidator<MoveComponentNodeCommand>
{
    public MoveComponentNodeValidator()
    {
        RuleFor(x => x.NewOrder).GreaterThanOrEqualTo(0);
    }
}
