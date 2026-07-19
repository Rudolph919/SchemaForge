using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Organizations.Queries.GetTeam;

public sealed record GetTeamQuery(Guid TeamId) : IQuery<Result<TeamDetail>>;

public sealed record TeamDetail(
    Guid Id, string Name, string? Description, IReadOnlyList<TeamMemberDetail> Members);

// Deliberately just UserId/JoinedAt, not Email/DisplayName - resolving that requires a join this
// query doesn't need to own; a caller with both this and ListOrganizationMembersQuery's results
// can cross-reference by UserId rather than every team-detail call re-fetching user rows it
// probably already has.
public sealed record TeamMemberDetail(Guid UserId, DateTimeOffset JoinedAt);
