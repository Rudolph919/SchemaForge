using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Queries.GetSchemaVersionDocumentation;

public sealed record GetSchemaVersionDocumentationQuery(Guid SchemaVersionId, string Format) : IQuery<Result<string>>;
