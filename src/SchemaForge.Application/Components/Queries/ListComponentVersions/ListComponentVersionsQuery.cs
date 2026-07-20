using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Components.Queries.ListComponentVersions;

// Headers only, no node tree - same reasoning as ListSchemaVersionsQuery.
public sealed record ListComponentVersionsQuery(Guid ComponentDefinitionId) : IQuery<Result<IReadOnlyList<ComponentVersionSummary>>>;
