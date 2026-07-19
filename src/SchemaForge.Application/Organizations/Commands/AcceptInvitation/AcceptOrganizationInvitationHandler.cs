using MediatR;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Organizations.Commands.AcceptInvitation;

public sealed class AcceptOrganizationInvitationHandler(
    IOrganizationMembershipRepository membershipRepository,
    ICurrentUserContext currentUserContext,
    ITenantContext tenantContext)
    : IRequestHandler<AcceptOrganizationInvitationCommand, Result>
{
    public async Task<Result> Handle(AcceptOrganizationInvitationCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserContext.UserId!.Value;

        // GetByIdForUserAsync filters by userId in the query itself (not a post-load check), so
        // there's no separate "is this actually mine" step needed here - the database simply
        // never returns another user's membership through this path.
        var membership = await membershipRepository.GetByIdForUserAsync(
            request.MembershipId, userId, cancellationToken);

        if (membership is null)
        {
            return Result.Failure(Error.NotFound("OrganizationMembership.NotFound", "No such invitation."));
        }

        // The caller's ambient tenant (from their JWT's org_id) is whatever org they're already
        // active in - not necessarily this invitation's org, which is exactly the case an invite
        // exists to bridge. Same bootstrapping need as Registration (see ITenantContext): without
        // this, the RLS policy's WITH CHECK clause (deliberately not given the self-lookup
        // exception - see the AllowSelfMembershipLookupForLogin migration) rejects the UPDATE
        // outright, since it only permits organization_id = app.current_tenant_id.
        tenantContext.SetTenant(membership.OrganizationId);

        return membership.Accept();
    }
}
