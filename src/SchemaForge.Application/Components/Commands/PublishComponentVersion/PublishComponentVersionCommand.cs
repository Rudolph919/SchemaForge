using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Components.Commands.PublishComponentVersion;

public sealed record PublishComponentVersionCommand(Guid ComponentVersionId) : ICommand<Result>;
