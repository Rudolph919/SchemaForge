using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Commands.DeprecateSchemaVersion;

public sealed record DeprecateSchemaVersionCommand(Guid SchemaVersionId) : ICommand<Result>;
