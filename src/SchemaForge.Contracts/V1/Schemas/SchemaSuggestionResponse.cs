namespace SchemaForge.Contracts.V1.Schemas;

public sealed record SuggestedNodeResponse(
    Guid Id,
    string? PropertyName,
    NodeKind Kind,
    string? Description,
    decimal Confidence,
    IReadOnlyList<SuggestedNodeResponse> Children);

public sealed record SchemaSuggestionResponse(
    string ProviderName, decimal? OverallConfidence, IReadOnlyList<SuggestedNodeResponse> Nodes);
