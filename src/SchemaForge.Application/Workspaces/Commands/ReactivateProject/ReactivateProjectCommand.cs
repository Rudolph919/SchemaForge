using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Workspaces.Commands.ReactivateProject;

public sealed record ReactivateProjectCommand(Guid ProjectId) : ICommand<Result>;
