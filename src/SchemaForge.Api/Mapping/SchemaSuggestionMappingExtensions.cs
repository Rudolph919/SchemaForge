using SchemaForge.Contracts.V1.Schemas;
using DomainSchemaSuggestion = SchemaForge.Domain.Schemas.SchemaSuggestion;
using DomainSuggestedNode = SchemaForge.Domain.Schemas.SuggestedNode;

namespace SchemaForge.Api.Mapping;

public static class SchemaSuggestionMappingExtensions
{
    public static SchemaSuggestionResponse ToResponse(this DomainSchemaSuggestion suggestion) => new(
        suggestion.ProviderName, suggestion.OverallConfidence, suggestion.Nodes.Select(ToResponse).ToList());

    private static SuggestedNodeResponse ToResponse(this DomainSuggestedNode node) => new(
        node.Id, node.PropertyName, node.Kind.ToContract(), node.Description, node.Confidence,
        node.Children.Select(ToResponse).ToList());
}
