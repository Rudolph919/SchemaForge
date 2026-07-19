using MediatR;
using SchemaForge.Domain.Organizations;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Organizations.Commands.ChangeMemberRole;

public sealed class ChangeOrganizationMemberRoleHandler(
    IOrganizationMembershipRepository membershipRepository, IOrganizationOwnershipGuard ownershipGuard)
    : IRequestHandler<ChangeOrganizationMemberRoleCommand, Result>
{
    public async Task<Result> Handle(ChangeOrganizationMemberRoleCommand request, CancellationToken cancellationToken)
    {
        var membership = await membershipRepository.GetByIdAsync(request.MembershipId, cancellationToken);

        if (membership is null)
        {
            return Result.Failure(Error.NotFound("OrganizationMembership.NotFound", "No such membership."));
        }

        var isDemotingAnActiveOwner =
            membership.Role == OrganizationRole.Owner
            && membership.Status == MembershipStatus.Active
            && request.NewRole != OrganizationRole.Owner;

        if (isDemotingAnActiveOwner)
        {
            var hasAnotherOwner = await ownershipGuard.HasAnotherActiveOwnerAsync(
                membership.OrganizationId, membership.Id, cancellationToken);

            if (!hasAnotherOwner)
            {
                return Result.Failure(Error.Validation(
                    "Organization.LastOwner", "Cannot demote the organization's only remaining Owner."));
            }
        }

        return membership.ChangeRole(request.NewRole);
    }
}
