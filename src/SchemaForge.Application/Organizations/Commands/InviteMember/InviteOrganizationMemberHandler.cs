using MediatR;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Application.Identity;
using SchemaForge.Domain.Organizations;
using SchemaForge.SharedKernel;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.Application.Organizations.Commands.InviteMember;

public sealed class InviteOrganizationMemberHandler(
    IUserRepository userRepository,
    IOrganizationMembershipRepository membershipRepository,
    ITenantContext tenantContext)
    : IRequestHandler<InviteOrganizationMemberCommand, Result<InviteOrganizationMemberResult>>
{
    public async Task<Result<InviteOrganizationMemberResult>> Handle(
        InviteOrganizationMemberCommand request, CancellationToken cancellationToken)
    {
        var organizationId = tenantContext.CurrentTenantId!.Value;
        var email = EmailAddress.Create(request.Email);

        var invitee = await userRepository.GetByEmailAsync(email, cancellationToken);
        if (invitee is null)
        {
            return Result<InviteOrganizationMemberResult>.Failure(Error.NotFound(
                "User.NotFound",
                "No SchemaForge account exists with this email. They'll need to register first."));
        }

        // Checks for ANY existing membership regardless of status, matching the DB's unique
        // (organization_id, user_id) constraint from Phase 0 - a membership row is never
        // deleted, only status-transitioned, so a previously revoked member can't currently be
        // re-invited through this path (that would need to update the existing row rather than
        // insert a new one, a genuinely different operation deliberately deferred rather than
        // rushed here).
        if (await membershipRepository.ExistsForUserAsync(organizationId, invitee.Id, cancellationToken))
        {
            return Result<InviteOrganizationMemberResult>.Failure(Error.Conflict(
                "OrganizationMembership.AlreadyExists",
                "This user already has a membership record in this organization."));
        }

        var membership = OrganizationMembership.Invite(organizationId, invitee.Id, request.Role);
        await membershipRepository.AddAsync(membership, cancellationToken);

        return new InviteOrganizationMemberResult(membership.Id);
    }
}
