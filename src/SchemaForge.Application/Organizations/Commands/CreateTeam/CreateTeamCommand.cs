using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Organizations.Commands.CreateTeam;

public sealed record CreateTeamCommand(string Name, string? Description) : ICommand<Result<CreateTeamResult>>;

public sealed record CreateTeamResult(Guid TeamId);
