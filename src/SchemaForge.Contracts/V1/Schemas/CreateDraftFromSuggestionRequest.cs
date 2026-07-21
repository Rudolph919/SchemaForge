namespace SchemaForge.Contracts.V1.Schemas;

// The client resends the exact SchemaSuggestionResponse it got back from suggest-schema, plus
// which SuggestedNodeResponse.Id values were accepted - the suggestion is never persisted
// server-side, so there's nothing else to look it up by.
public sealed record CreateDraftFromSuggestionRequest(
    SchemaSuggestionResponse Suggestion,
    IReadOnlyList<Guid> AcceptedNodeIds,
    VersionBumpKind BumpKind,
    string? ChangeSummary);
