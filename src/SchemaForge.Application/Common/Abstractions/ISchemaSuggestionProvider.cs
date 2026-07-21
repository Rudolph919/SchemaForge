using SchemaForge.Domain.Schemas;
using SchemaForge.Domain.Workspaces;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Common.Abstractions;

// Step 9 §2's flagship seam: AI schema suggestion becomes an Application-layer port with an
// Infrastructure-layer adapter, swappable without touching Domain/Application logic. Takes the
// SourceDocument entity itself (not raw bytes) - a real implementation fetches the file via
// IFileStorage internally using document.StorageKey if and when it needs to; NullSchemaSuggestionProvider
// (the only implementation today) never touches storage at all, since it always fails fast.
public interface ISchemaSuggestionProvider
{
    Task<Result<SchemaSuggestion>> SuggestAsync(SourceDocument document, CancellationToken cancellationToken);
}
