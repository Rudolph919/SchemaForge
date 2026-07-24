using FluentValidation;

namespace SchemaForge.Application.Schemas.Commands.ReparentSchemaNode;

public sealed class ReparentSchemaNodeValidator : AbstractValidator<ReparentSchemaNodeCommand>
{
    public ReparentSchemaNodeValidator()
    {
        RuleFor(x => x.SchemaVersionId).NotEmpty();
        RuleFor(x => x.NodeId).NotEmpty();
        RuleFor(x => x.NewParentNodeId).NotEmpty();
        RuleFor(x => x.AttachmentKind).NotNull().IsInEnum();

        RuleFor(x => x.PropertyName)
            .NotEmpty()
            .When(x => x.AttachmentKind == NodeAttachmentKind.ObjectProperty)
            .WithMessage("Property name is required when reparenting as an object property.");

        RuleFor(x => x.PropertyName)
            .Empty()
            .When(x => x.AttachmentKind != NodeAttachmentKind.ObjectProperty)
            .WithMessage("Property name only applies when reparenting as an object property.");
    }
}
