using SchemaForge.Application.Common.Messaging;
using SchemaForge.Application.Schemas;
using SchemaForge.SharedKernel;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.Application.Components.Commands.CreateComponentVersion;

public sealed record CreateComponentVersionCommand(Guid ComponentDefinitionId, VersionBumpKind BumpKind, string? ChangeSummary)
    : ICommand<Result<CreateComponentVersionResult>>;

public sealed record CreateComponentVersionResult(Guid ComponentVersionId, SemVer VersionNumber);
