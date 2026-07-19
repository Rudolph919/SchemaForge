namespace SchemaForge.Contracts.V1.Teams;

public sealed record TeamMemberResponse(Guid UserId, DateTimeOffset JoinedAt);
