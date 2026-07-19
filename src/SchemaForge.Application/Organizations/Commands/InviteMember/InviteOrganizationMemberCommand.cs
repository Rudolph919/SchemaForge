using SchemaForge.Application.Common.Messaging;
using SchemaForge.Domain.Organizations;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Organizations.Commands.InviteMember;

// No OrganizationId parameter, deliberately: the org is always the caller's own current tenant
// (ITenantContext.CurrentTenantId, established from the JWT), never a caller-supplied value that
// would need cross-checking against it. One source of truth, not two that could disagree.
public sealed record InviteOrganizationMemberCommand(string Email, OrganizationRole Role)
    : ICommand<Result<InviteOrganizationMemberResult>>;

public sealed record InviteOrganizationMemberResult(Guid MembershipId);
