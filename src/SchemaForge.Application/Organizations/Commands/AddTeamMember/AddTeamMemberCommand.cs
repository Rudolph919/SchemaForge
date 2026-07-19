using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Organizations.Commands.AddTeamMember;

public sealed record AddTeamMemberCommand(Guid TeamId, Guid UserId) : ICommand<Result>;
