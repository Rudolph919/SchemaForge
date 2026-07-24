using FluentValidation;

namespace SchemaForge.Application.Identity.Commands.RefreshAccessToken;

public sealed class RefreshAccessTokenValidator : AbstractValidator<RefreshAccessTokenCommand>
{
    public RefreshAccessTokenValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
