using SchemaForge.Application.Common.Messaging;
using SchemaForge.Domain.Workspaces;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Workspaces.Queries.ListProjects;

public sealed record ListProjectsQuery : IQuery<Result<IReadOnlyList<ProjectSummary>>>;

public sealed record ProjectSummary(Guid Id, string Name, string? Description, ProjectStatus Status);
