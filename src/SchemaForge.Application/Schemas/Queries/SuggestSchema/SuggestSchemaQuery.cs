using SchemaForge.Application.Common.Messaging;
using SchemaForge.Domain.Schemas;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Schemas.Queries.SuggestSchema;

// Qry-shaped like Step 6 §1.4's /validate - no persistence side effect of its own (SchemaSuggestion
// is never persisted, Step 9 §2), so this is a genuine IQuery despite the endpoint being a POST
// (an expensive external call isn't idempotent-safe-by-default the way a GET implies, matching
// the same POST-despite-being-a-query shape /validate already established).
public sealed record SuggestSchemaQuery(Guid SourceDocumentId) : IQuery<Result<SchemaSuggestion>>;
