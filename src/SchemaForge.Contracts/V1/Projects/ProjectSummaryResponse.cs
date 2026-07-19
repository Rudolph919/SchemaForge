namespace SchemaForge.Contracts.V1.Projects;

public sealed record ProjectSummaryResponse(Guid Id, string Name, string? Description, ProjectStatus Status);
