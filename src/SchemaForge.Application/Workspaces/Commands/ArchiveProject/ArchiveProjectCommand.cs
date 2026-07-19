using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Workspaces.Commands.ArchiveProject;

public sealed record ArchiveProjectCommand(Guid ProjectId) : ICommand<Result>;
