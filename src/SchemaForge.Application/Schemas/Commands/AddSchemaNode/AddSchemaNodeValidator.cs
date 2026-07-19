using FluentValidation;

namespace SchemaForge.Application.Schemas.Commands.AddSchemaNode;

public sealed class AddSchemaNodeValidator : AbstractValidator<AddSchemaNodeCommand>
{
    public AddSchemaNodeValidator()
    {
        RuleFor(x => x.SchemaVersionId).NotEmpty();
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
