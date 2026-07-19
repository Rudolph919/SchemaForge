using MediatR;
using SchemaForge.Domain.Organizations;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Organizations.Commands.RevokeMember;

public sealed class RevokeOrganizationMemberHandler(
    IOrganizationMembershipRepository membershipRepository, IOrganizationOwnershipGuard ownershipGuard)
    : IRequestHandler<RevokeOrganizationMemberCommand, Result>
{
    public async Task<Result> Handle(RevokeOrganizationMemberCommand request, CancellationToken cancellationToken)
    {
        var membership = await membershipRepository.GetByIdAsync(request.MembershipId, cancellationToken);

        if (membership is null)
        {
            return Result.Failure(Error.NotFound("OrganizationMembership.NotFound", "No such membership."));
        }

        var isRevokingAnActiveOwner =
            membership.Role == OrganizationRole.Owner && membership.Status == MembershipStatus.Active;

        if (isRevokingAnActiveOwner)
        {
            var hasAnotherOwner = await ownershipGuard.HasAnotherActiveOwnerAsync(
                membership.OrganizationId, membership.Id, cancellationToken);

            if (!hasAnotherOwner)
            {
                return Result.Failure(Error.Validation(
                    "Organization.LastOwner", "Cannot revoke the organization's only remaining Owner."));
            }
        }

        return membership.Revoke();
    }
}
