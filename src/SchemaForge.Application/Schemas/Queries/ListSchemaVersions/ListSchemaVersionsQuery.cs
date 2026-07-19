using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Queries.ListSchemaVersions;

// Headers only, no node tree (Step 6 §2.4) - ISchemaVersionRepository.GetAllForSchemaDefinitionAsync
// is a lean projection that never touches the jsonb columns.
public sealed record ListSchemaVersionsQuery(Guid SchemaDefinitionId) : IQuery<Result<IReadOnlyList<SchemaVersionSummary>>>;
