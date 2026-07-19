namespace SchemaForge.Contracts.V1.Teams;

public sealed record TeamSummaryResponse(Guid Id, string Name, string? Description, int MemberCount);
