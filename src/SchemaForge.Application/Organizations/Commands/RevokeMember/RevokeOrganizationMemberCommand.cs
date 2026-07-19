using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Organizations.Commands.RevokeMember;

public sealed record RevokeOrganizationMemberCommand(Guid MembershipId) : ICommand<Result>;
