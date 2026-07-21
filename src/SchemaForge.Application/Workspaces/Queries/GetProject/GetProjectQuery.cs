using SchemaForge.Application.Common.Messaging;
using SchemaForge.Domain.Workspaces;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Workspaces.Queries.GetProject;

public sealed record GetProjectQuery(Guid ProjectId) : IQuery<Result<ProjectDetail>>;

public sealed record ProjectDetail(Guid Id, string Name, string? Description, ProjectStatus Status, uint RowVersion);
