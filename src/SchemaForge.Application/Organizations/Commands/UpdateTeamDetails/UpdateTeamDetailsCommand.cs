using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Organizations.Commands.UpdateTeamDetails;

public sealed record UpdateTeamDetailsCommand(Guid TeamId, string Name, string? Description) : ICommand<Result>;
