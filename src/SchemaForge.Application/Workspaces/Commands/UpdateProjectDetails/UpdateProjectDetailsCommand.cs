using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Workspaces.Commands.UpdateProjectDetails;

public sealed record UpdateProjectDetailsCommand(Guid ProjectId, string Name, string? Description) : ICommand<Result>;
