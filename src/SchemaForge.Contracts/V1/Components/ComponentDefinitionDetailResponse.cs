namespace SchemaForge.Contracts.V1.Components;

public sealed record ComponentDefinitionDetailResponse(Guid Id, Guid OrganizationId, string Name, string? Description);
