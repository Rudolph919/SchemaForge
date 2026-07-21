using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Domain.Schemas;
using SchemaForge.Domain.Workspaces;
using SchemaForge.SharedKernel;

namespace SchemaForge.Infrastructure.Ai;

// Step 9 §2: the whole pipeline - POST /source-documents/{id}/suggest-schema,
// CreateDraftFromSuggestion, the Designer's review UI - is built, wired end-to-end, and demoable
// today with this as the only implementation. A real provider (some multimodal LLM call reading
// the uploaded document and proposing structure) drops in later behind the same
// ISchemaSuggestionProvider interface with zero change to anything upstream of it - that's the
// entire point of the seam, not a placeholder to be embarrassed about.
public sealed class NullSchemaSuggestionProvider : ISchemaSuggestionProvider
{
    public Task<Result<SchemaSuggestion>> SuggestAsync(SourceDocument document, CancellationToken cancellationToken) =>
        Task.FromResult(Result<SchemaSuggestion>.Failure(Error.Unexpected(
            "SchemaSuggestion.NotConfigured", "AI schema suggestion is not configured for this environment.")));
}
