namespace SchemaForge.Contracts.V1.Schemas;

public sealed record CreateDraftFromSuggestionResponse(Guid SchemaVersionId, string VersionNumber, int AcceptedCount);
