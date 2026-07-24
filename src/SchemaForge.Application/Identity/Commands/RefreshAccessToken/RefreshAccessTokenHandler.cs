using System.Security.Cryptography;
using MediatR;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Application.Organizations;
using SchemaForge.Domain.Identity;
using SchemaForge.Domain.Organizations;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Identity.Commands.RefreshAccessToken;

public sealed class RefreshAccessTokenHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IUserRepository userRepository,
    IOrganizationMembershipRepository membershipRepository,
    IRefreshTokenHasher refreshTokenHasher,
    IJwtTokenService jwtTokenService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RefreshAccessTokenCommand, Result<RefreshAccessTokenResult>>
{
    // Same generic error for every failure mode (unknown hash, already-rotated-out, expired) -
    // telling an attacker which one it was would help them distinguish "guessed wrong" from
    // "found a real but stale token," the same anti-enumeration reasoning as Login's
    // InvalidCredentials.
    private static readonly Error InvalidRefreshToken =
        Error.Validation("Auth.InvalidRefreshToken", "Refresh token is invalid or expired.");

    public async Task<Result<RefreshAccessTokenResult>> Handle(
        RefreshAccessTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = refreshTokenHasher.Hash(request.RefreshToken);
        var storedToken = await refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (storedToken is null)
        {
            return Result<RefreshAccessTokenResult>.Failure(InvalidRefreshToken);
        }

        if (storedToken.RevokedAt is not null)
        {
            // Replaying a token that was already rotated out (or already used) only happens
            // legitimately never - treat it as the chain being compromised and kill every other
            // active token this user holds, forcing a fresh login everywhere.
            var activeTokens =
                await refreshTokenRepository.GetActiveByUserIdAsync(storedToken.UserId, cancellationToken);

            foreach (var activeToken in activeTokens)
            {
                activeToken.Revoke();
            }

            // TransactionBehavior only calls SaveChanges when the command succeeds (Step 6
            // §1.5's "commit or don't call SaveChanges" contract) - but the whole point of this
            // branch is a persisted side effect (mass revocation) on what's still a failure
            // result, so it has to be saved explicitly here rather than relying on the pipeline.
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<RefreshAccessTokenResult>.Failure(InvalidRefreshToken);
        }

        if (!storedToken.IsActive)
        {
            return Result<RefreshAccessTokenResult>.Failure(InvalidRefreshToken);
        }

        var user = await userRepository.GetByIdAsync(storedToken.UserId, cancellationToken);

        // Re-resolved rather than trusted from the stored token - membership/role may have
        // changed (or been revoked) since this refresh token was issued.
        var memberships = await membershipRepository.GetAllByUserIdAsync(storedToken.UserId, cancellationToken);
        var membership = memberships.SingleOrDefault(
            m => m.OrganizationId == storedToken.OrganizationId && m.Status == MembershipStatus.Active);

        if (user is null || membership is null)
        {
            return Result<RefreshAccessTokenResult>.Failure(InvalidRefreshToken);
        }

        var newRawToken = RandomNumberGenerator.GetHexString(64);
        var newToken = RefreshToken.Issue(user.Id, membership.OrganizationId, refreshTokenHasher.Hash(newRawToken));

        storedToken.Revoke(newToken.Id);
        await refreshTokenRepository.AddAsync(newToken, cancellationToken);

        var accessToken = jwtTokenService.GenerateAccessToken(user, membership.OrganizationId, membership.Role);

        return new RefreshAccessTokenResult(
            accessToken, newRawToken, user.Id, membership.OrganizationId, user.DisplayName);
    }
}
