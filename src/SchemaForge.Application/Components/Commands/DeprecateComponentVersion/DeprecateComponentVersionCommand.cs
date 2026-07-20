using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Components.Commands.DeprecateComponentVersion;

public sealed record DeprecateComponentVersionCommand(Guid ComponentVersionId) : ICommand<Result>;
