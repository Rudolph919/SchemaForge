using System.Security.Cryptography;
using MediatR;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Application.Organizations;
using SchemaForge.Domain.Identity;
using SchemaForge.SharedKernel;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.Application.Identity.Commands.Login;

public sealed class LoginHandler(
    IUserRepository userRepository,
    IOrganizationMembershipRepository membershipRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordHasher passwordHasher,
    IRefreshTokenHasher refreshTokenHasher,
    IJwtTokenService jwtTokenService) : IRequestHandler<LoginCommand, Result<LoginResult>>
{
    // Deliberately the same generic error for "no such user" and "wrong password" - revealing
    // which one it was lets an attacker enumerate registered emails.
    private static readonly Error InvalidCredentials =
        Error.Validation("Auth.InvalidCredentials", "Email or password is incorrect.");

    public async Task<Result<LoginResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = EmailAddress.Create(request.Email);
        var user = await userRepository.GetByEmailAsync(email, cancellationToken);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Result<LoginResult>.Failure(InvalidCredentials);
        }

        var membership = await membershipRepository.GetFirstByUserIdAsync(user.Id, cancellationToken);

        if (membership is null)
        {
            return Result<LoginResult>.Failure(Error.Unexpected(
                "User.NoOrganization", "This account has no organization membership."));
        }

        var accessToken = jwtTokenService.GenerateAccessToken(user, membership.OrganizationId, membership.Role);

        var rawRefreshToken = RandomNumberGenerator.GetHexString(64);
        var refreshToken = RefreshToken.Issue(
            user.Id, membership.OrganizationId, refreshTokenHasher.Hash(rawRefreshToken));
        await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        return new LoginResult(accessToken, rawRefreshToken, user.Id, membership.OrganizationId, user.DisplayName);
    }
}
