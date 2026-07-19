using SchemaForge.Application.Common.Messaging;
using SchemaForge.Domain.Organizations;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Organizations.Commands.ChangeMemberRole;

public sealed record ChangeOrganizationMemberRoleCommand(Guid MembershipId, OrganizationRole NewRole)
    : ICommand<Result>;
