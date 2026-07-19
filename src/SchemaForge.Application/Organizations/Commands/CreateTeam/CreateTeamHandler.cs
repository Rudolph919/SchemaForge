using MediatR;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Domain.Organizations;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Organizations.Commands.CreateTeam;

public sealed class CreateTeamHandler(ITeamRepository teamRepository, ITenantContext tenantContext)
    : IRequestHandler<CreateTeamCommand, Result<CreateTeamResult>>
{
    public async Task<Result<CreateTeamResult>> Handle(CreateTeamCommand request, CancellationToken cancellationToken)
    {
        if (await teamRepository.ExistsByNameAsync(request.Name, cancellationToken))
        {
            return Result<CreateTeamResult>.Failure(Error.Conflict(
                "Team.NameAlreadyExists", "A team with this name already exists in this organization."));
        }

        var team = Team.Create(tenantContext.CurrentTenantId!.Value, request.Name, request.Description);
        await teamRepository.AddAsync(team, cancellationToken);

        return new CreateTeamResult(team.Id);
    }
}
