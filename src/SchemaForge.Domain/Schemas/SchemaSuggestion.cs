namespace SchemaForge.Domain.Schemas;

// A quarantined proposal, not a domain aggregate (Step 9 §2) - the provider that produces this
// never gets write access to a real SchemaVersion. It's inert data a human reviews; only
// CreateDraftFromSuggestionHandler ever turns it into a real Draft, and it does so by calling the
// exact same SchemaVersion.AddObjectProperty(...) methods a human editing in the Designer would
// call, so every invariant the aggregate enforces applies identically to an AI-suggested node.
// Never persisted, same as SchemaDiff.
public sealed record SchemaSuggestion(
    string ProviderName, decimal? OverallConfidence, IReadOnlyList<SuggestedNode> Nodes);

// Id is assigned by the provider (or NullSchemaSuggestionProvider) when the suggestion is built,
// not by anything client-supplied - it's what CreateDraftFromSuggestionCommand's AcceptedNodeIds
// reference, since a suggestion has no other stable identity to select against (it's never
// persisted, so there's no database id to reuse).
public sealed record SuggestedNode(
    Guid Id,
    string? PropertyName,
    NodeKind Kind,
    string? Description,
    decimal Confidence,
    IReadOnlyList<SuggestedNode> Children);
