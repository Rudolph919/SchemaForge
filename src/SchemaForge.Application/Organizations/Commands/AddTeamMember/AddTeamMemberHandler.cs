using MediatR;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Organizations.Commands.AddTeamMember;

public sealed class AddTeamMemberHandler(
    ITeamRepository teamRepository,
    IOrganizationMembershipRepository membershipRepository,
    ITenantContext tenantContext)
    : IRequestHandler<AddTeamMemberCommand, Result>
{
    public async Task<Result> Handle(AddTeamMemberCommand request, CancellationToken cancellationToken)
    {
        var team = await teamRepository.GetByIdAsync(request.TeamId, cancellationToken);

        if (team is null)
        {
            return Result.Failure(Error.NotFound("Team.NotFound", "No such team."));
        }

        // Cross-aggregate check (Step 3 §4): must happen here, before calling team.AddMember(),
        // since Team can't reach into OrganizationMembership itself. Team.AddMember only enforces
        // what's genuinely internal to it (no duplicate membership).
        var isActiveOrgMember = await membershipRepository.IsActiveMemberAsync(
            tenantContext.CurrentTenantId!.Value, request.UserId, cancellationToken);

        if (!isActiveOrgMember)
        {
            return Result.Failure(Error.Validation(
                "Team.UserNotAnOrganizationMember",
                "This user must be an active member of the organization before joining a team."));
        }

        return team.AddMember(request.UserId);
    }
}
