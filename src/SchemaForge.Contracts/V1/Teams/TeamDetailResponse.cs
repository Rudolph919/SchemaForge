namespace SchemaForge.Contracts.V1.Teams;

public sealed record TeamDetailResponse(
    Guid Id, string Name, string? Description, IReadOnlyList<TeamMemberResponse> Members);
