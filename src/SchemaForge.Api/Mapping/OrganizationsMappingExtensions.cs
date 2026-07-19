using SchemaForge.Application.Organizations;
using SchemaForge.Application.Organizations.Commands.ChangeMemberRole;
using SchemaForge.Application.Organizations.Commands.InviteMember;
using SchemaForge.Contracts.V1.Organizations;
using DomainMembershipStatus = SchemaForge.Domain.Organizations.MembershipStatus;
using DomainOrganizationRole = SchemaForge.Domain.Organizations.OrganizationRole;

namespace SchemaForge.Api.Mapping;

public static class OrganizationsMappingExtensions
{
    public static InviteOrganizationMemberCommand ToCommand(this InviteMemberRequest request) =>
        new(request.Email, request.Role.ToDomain());

    public static InviteMemberResponse ToResponse(this InviteOrganizationMemberResult result) =>
        new(result.MembershipId);

    public static ChangeOrganizationMemberRoleCommand ToCommand(this ChangeMemberRoleRequest request, Guid membershipId) =>
        new(membershipId, request.NewRole.ToDomain());

    public static OrganizationMemberResponse ToResponse(this OrganizationMemberSummary summary) =>
        new(
            summary.MembershipId, summary.UserId, summary.Email, summary.DisplayName,
            summary.Role.ToContract(), summary.Status.ToContract());

    public static MembershipResponse ToResponse(this MembershipWithOrganizationSummary summary) =>
        new(
            summary.MembershipId, summary.OrganizationId, summary.OrganizationName, summary.OrganizationSlug,
            summary.Role.ToContract(), summary.Status.ToContract());

    private static DomainOrganizationRole ToDomain(this OrganizationRole role) => role switch
    {
        OrganizationRole.Owner => DomainOrganizationRole.Owner,
        OrganizationRole.Admin => DomainOrganizationRole.Admin,
        OrganizationRole.Member => DomainOrganizationRole.Member,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown organization role.")
    };

    private static OrganizationRole ToContract(this DomainOrganizationRole role) => role switch
    {
        DomainOrganizationRole.Owner => OrganizationRole.Owner,
        DomainOrganizationRole.Admin => OrganizationRole.Admin,
        DomainOrganizationRole.Member => OrganizationRole.Member,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown organization role.")
    };

    private static MembershipStatus ToContract(this DomainMembershipStatus status) => status switch
    {
        DomainMembershipStatus.Invited => MembershipStatus.Invited,
        DomainMembershipStatus.Active => MembershipStatus.Active,
        DomainMembershipStatus.Revoked => MembershipStatus.Revoked,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown membership status.")
    };
}
