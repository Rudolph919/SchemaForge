using FluentValidation;
using SchemaForge.Application.Schemas;

namespace SchemaForge.Application.Components.Commands.ReparentComponentNode;

public sealed class ReparentComponentNodeValidator : AbstractValidator<ReparentComponentNodeCommand>
{
    public ReparentComponentNodeValidator()
    {
        RuleFor(x => x.ComponentVersionId).NotEmpty();
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
