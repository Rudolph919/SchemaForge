using MediatR;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Application.Organizations;
using SchemaForge.Domain.Organizations;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Identity.Commands.SwitchOrganization;

public sealed class SwitchOrganizationHandler(
    IUserRepository userRepository,
    IOrganizationMembershipRepository membershipRepository,
    ICurrentUserContext currentUserContext,
    IJwtTokenService jwtTokenService)
    : IRequestHandler<SwitchOrganizationQuery, Result<SwitchOrganizationResult>>
{
    public async Task<Result<SwitchOrganizationResult>> Handle(
        SwitchOrganizationQuery request, CancellationToken cancellationToken)
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

        return new SwitchOrganizationResult(accessToken, targetMembership.OrganizationId, user!.DisplayName);
    }
}
