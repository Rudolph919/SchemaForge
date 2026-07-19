namespace SchemaForge.Contracts.V1.Schemas;

public sealed record SchemaDefinitionDetailResponse(
    Guid Id, Guid ProjectId, string Name, string? Description, IReadOnlyList<string> Tags);
