using SchemaForge.Application.Common.Messaging;
using SchemaForge.Domain.Schemas;
using SchemaForge.SharedKernel;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.Application.Schemas.Commands.CreateDraftFromSuggestion;

// Step 9 §2: SchemaSuggestion is never persisted, so the client resends the exact suggestion it
// got back from SuggestSchemaQuery, plus which SuggestedNode.Id values a human accepted -
// there's no server-side record to look up instead. AcceptedNodeIds not covering a node also
// prunes that node's entire subtree (a rejected node has no valid parent to attach children to
// in the resulting version), not just that one node.
public sealed record CreateDraftFromSuggestionCommand(
    Guid SchemaDefinitionId,
    SchemaSuggestion Suggestion,
    IReadOnlyList<Guid> AcceptedNodeIds,
    VersionBumpKind BumpKind,
    string? ChangeSummary) : ICommand<Result<CreateDraftFromSuggestionResult>>;

public sealed record CreateDraftFromSuggestionResult(Guid SchemaVersionId, SemVer VersionNumber, int AcceptedCount);
