using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Organizations.Commands.RemoveTeamMember;

public sealed record RemoveTeamMemberCommand(Guid TeamId, Guid UserId) : ICommand<Result>;
