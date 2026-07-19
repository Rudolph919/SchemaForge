namespace SchemaForge.Contracts.V1.Projects;

public sealed record ProjectDetailResponse(Guid Id, string Name, string? Description, ProjectStatus Status);
