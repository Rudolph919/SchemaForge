using MediatR;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Organizations.Commands.AcceptInvitation;

public sealed class AcceptOrganizationInvitationHandler(
    IOrganizationMembershipRepository membershipRepository, ICurrentUserContext currentUserContext)
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

        return membership.Accept();
    }
}
