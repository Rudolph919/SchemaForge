using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Components.Queries.GetComponentLibrary;

// No parameters - Components are Organization-scoped, so the org comes from ambient tenant
// context (ITenantContext), not a route parameter, unlike GetSchemaLibraryQuery's ProjectId.
// Unpaginated for now, matching GetSchemaLibraryQuery's own reasoning.
public sealed record GetComponentLibraryQuery : IQuery<Result<IReadOnlyList<ComponentDefinitionSummary>>>;

public sealed record ComponentDefinitionSummary(Guid Id, string Name, string? Description);
