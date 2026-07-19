using FluentValidation;

namespace SchemaForge.Application.Schemas.Commands.UpdateSchemaDefinitionDetails;

public sealed class UpdateSchemaDefinitionDetailsValidator : AbstractValidator<UpdateSchemaDefinitionDetailsCommand>
{
    public UpdateSchemaDefinitionDetailsValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleForEach(x => x.Tags).NotEmpty().MaximumLength(50);
    }
}
