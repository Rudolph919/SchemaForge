using FluentValidation;
using SchemaForge.Application.Schemas;

namespace SchemaForge.Application.Components.Commands.AddComponentNode;

public sealed class AddComponentNodeValidator : AbstractValidator<AddComponentNodeCommand>
{
    public AddComponentNodeValidator()
    {
        RuleFor(x => x.ComponentVersionId).NotEmpty();
        RuleFor(x => x.ParentNodeId).NotEmpty();
        RuleFor(x => x.AttachmentKind).IsInEnum();

        RuleFor(x => x.PropertyName)
            .NotEmpty()
            .When(x => x.AttachmentKind == NodeAttachmentKind.ObjectProperty)
            .WithMessage("Property name is required when adding an object property.");

        RuleFor(x => x.PropertyName)
            .Empty()
            .When(x => x.AttachmentKind != NodeAttachmentKind.ObjectProperty)
            .WithMessage("Property name only applies when adding an object property.");
    }
}
