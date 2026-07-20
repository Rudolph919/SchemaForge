using System.Text.Json;
using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.Application.Schemas.Commands.ImportSchemaVersion;

public sealed record ImportSchemaVersionCommand(
    Guid SchemaDefinitionId, JsonElement SchemaDocument, VersionBumpKind BumpKind, string? ChangeSummary)
    : ICommand<Result<ImportSchemaVersionResult>>;

public sealed record ImportSchemaVersionResult(Guid SchemaVersionId, SemVer VersionNumber);
