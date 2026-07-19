using FluentValidation;

namespace SchemaForge.Application.Workspaces.Commands.UploadSourceDocument;

public sealed class UploadSourceDocumentValidator : AbstractValidator<UploadSourceDocumentCommand>
{
    public UploadSourceDocumentValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(255);
        RuleFor(x => x.SizeBytes).GreaterThan(0);
    }
}
