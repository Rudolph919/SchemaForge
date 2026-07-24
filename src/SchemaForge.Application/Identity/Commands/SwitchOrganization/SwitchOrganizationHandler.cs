using System.Security.Cryptography;
using MediatR;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Application.Organizations;
using SchemaForge.Domain.Identity;
using SchemaForge.Domain.Organizations;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Identity.Commands.SwitchOrganization;

public sealed class SwitchOrganizationHandler(
    IUserRepository userRepository,
    IOrganizationMembershipRepository membershipRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IRefreshTokenHasher refreshTokenHasher,
    ICurrentUserContext currentUserContext,
    IJwtTokenService jwtTokenService)
    : IRequestHandler<SwitchOrganizationCommand, Result<SwitchOrganizationResult>>
{
    public async Task<Result<SwitchOrganizationResult>> Handle(
        SwitchOrganizationCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserContext.UserId!.Value;

        // Reuses the same query ListMyMembershipsQuery is built on, rather than adding a new
        // repository method just to look up one (organizationId, userId) pair - the caller's
        // full membership list is already exactly what's needed to both find the target and
        // confirm it's actually theirs to switch into.
        var memberships = await membershipRepository.GetAllByUserIdAsync(userId, cancellationToken);
        var targetMembership = memberships.SingleOrDefault(
            m => m.OrganizationId == request.OrganizationId && m.Status == MembershipStatus.Active);

        if (targetMembership is null)
        {
            return Result<SwitchOrganizationResult>.Failure(Error.Forbidden(
                "Organization.NotAnActiveMember", "You are not an active member of this organization."));
        }

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        var accessToken = jwtTokenService.GenerateAccessToken(user!, targetMembership.OrganizationId, targetMembership.Role);

        // A fresh refresh token scoped to the newly-active org - the one the caller arrived with
        // still points at their previous org and is left alone; it stays valid until it naturally
        // expires, which is no more than the caller could achieve by just switching back.
        var rawRefreshToken = RandomNumberGenerator.GetHexString(64);
        var refreshToken = RefreshToken.Issue(
            user!.Id, targetMembership.OrganizationId, refreshTokenHasher.Hash(rawRefreshToken));
        await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        return new SwitchOrganizationResult(
            accessToken, rawRefreshToken, targetMembership.OrganizationId, user.DisplayName);
    }
}
