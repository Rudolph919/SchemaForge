using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Organizations.Commands.AcceptInvitation;

public sealed record AcceptOrganizationInvitationCommand(Guid MembershipId) : ICommand<Result>;
