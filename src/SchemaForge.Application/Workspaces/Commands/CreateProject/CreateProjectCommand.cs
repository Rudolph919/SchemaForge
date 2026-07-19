using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Workspaces.Commands.CreateProject;

public sealed record CreateProjectCommand(string Name, string? Description) : ICommand<Result<CreateProjectResult>>;

public sealed record CreateProjectResult(Guid ProjectId);
