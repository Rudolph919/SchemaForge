using SchemaForge.Application.Common.Messaging;
using SchemaForge.Domain.Schemas;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Queries.GetSchemaDiff;

public sealed record GetSchemaDiffQuery(Guid SchemaVersionId, Guid AgainstSchemaVersionId) : IQuery<Result<SchemaDiff>>;
