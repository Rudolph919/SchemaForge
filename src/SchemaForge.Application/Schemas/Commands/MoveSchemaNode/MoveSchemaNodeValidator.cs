using FluentValidation;

namespace SchemaForge.Application.Schemas.Commands.MoveSchemaNode;

public sealed class MoveSchemaNodeValidator : AbstractValidator<MoveSchemaNodeCommand>
{
    public MoveSchemaNodeValidator()
    {
        RuleFor(x => x.NewOrder).GreaterThanOrEqualTo(0);
    }
}
