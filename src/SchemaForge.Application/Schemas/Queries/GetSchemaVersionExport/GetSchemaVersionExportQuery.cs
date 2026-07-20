using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Queries.GetSchemaVersionExport;

public sealed record GetSchemaVersionExportQuery(Guid SchemaVersionId, string Format) : IQuery<Result<string>>;
