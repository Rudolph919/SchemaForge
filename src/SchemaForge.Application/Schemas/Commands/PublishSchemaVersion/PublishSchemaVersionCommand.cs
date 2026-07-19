using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Commands.PublishSchemaVersion;

public sealed record PublishSchemaVersionCommand(Guid SchemaVersionId) : ICommand<Result>;
