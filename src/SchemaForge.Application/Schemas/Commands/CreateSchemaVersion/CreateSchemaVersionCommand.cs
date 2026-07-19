using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.Application.Schemas.Commands.CreateSchemaVersion;

public sealed record CreateSchemaVersionCommand(Guid SchemaDefinitionId, VersionBumpKind BumpKind, string? ChangeSummary)
    : ICommand<Result<CreateSchemaVersionResult>>;

public sealed record CreateSchemaVersionResult(Guid SchemaVersionId, SemVer VersionNumber);
