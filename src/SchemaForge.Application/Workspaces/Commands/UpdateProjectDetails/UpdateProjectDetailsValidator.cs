using FluentValidation;

namespace SchemaForge.Application.Workspaces.Commands.UpdateProjectDetails;

public sealed class UpdateProjectDetailsValidator : AbstractValidator<UpdateProjectDetailsCommand>
{
    public UpdateProjectDetailsValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}
