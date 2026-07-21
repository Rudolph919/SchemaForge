using SchemaForge.Application.Schemas.Commands.CreateDraftFromSuggestion;
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

    public static CreateDraftFromSuggestionCommand ToCommand(this CreateDraftFromSuggestionRequest request, Guid schemaDefinitionId) =>
        new(schemaDefinitionId, request.Suggestion.ToDomain(), request.AcceptedNodeIds, request.BumpKind.ToDomain(), request.ChangeSummary);

    private static DomainSchemaSuggestion ToDomain(this SchemaSuggestionResponse suggestion) => new(
        suggestion.ProviderName, suggestion.OverallConfidence, suggestion.Nodes.Select(ToDomain).ToList());

    private static DomainSuggestedNode ToDomain(this SuggestedNodeResponse node) => new(
        node.Id, node.PropertyName, node.Kind.ToDomain(), node.Description, node.Confidence,
        node.Children.Select(ToDomain).ToList());

    public static CreateDraftFromSuggestionResponse ToResponse(this CreateDraftFromSuggestionResult result) =>
        new(result.SchemaVersionId, result.VersionNumber.ToString(), result.AcceptedCount);
}
