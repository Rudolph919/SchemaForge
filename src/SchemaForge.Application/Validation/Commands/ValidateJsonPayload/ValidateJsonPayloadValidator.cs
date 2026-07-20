using FluentValidation;

namespace SchemaForge.Application.Validation.Commands.ValidateJsonPayload;

public sealed class ValidateJsonPayloadValidator : AbstractValidator<ValidateJsonPayloadCommand>
{
    public ValidateJsonPayloadValidator()
    {
        RuleFor(x => x.SchemaVersionId).NotEmpty();
    }
}
