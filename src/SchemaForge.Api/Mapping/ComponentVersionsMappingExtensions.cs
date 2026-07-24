using SchemaForge.Application.Components;
using SchemaForge.Application.Components.Commands.AddComponentLocalDefinition;
using SchemaForge.Application.Components.Commands.AddComponentNode;
using SchemaForge.Application.Components.Commands.CreateComponentVersion;
using SchemaForge.Application.Components.Commands.MoveComponentNode;
using SchemaForge.Application.Components.Commands.ReparentComponentNode;
using SchemaForge.Application.Components.Queries.GetComponentVersion;
using SchemaForge.Contracts.V1.Components;
using SchemaForge.Contracts.V1.Schemas;

namespace SchemaForge.Api.Mapping;

// Reuses SchemaVersionsMappingExtensions' internal node-tree/enum mapping helpers directly
// (ToResponse(this SchemaNode), ToResponse(this LocalDefinition), every constraint DTO mapping,
// every Contract<->Domain enum converter) rather than duplicating ~200 lines of recursive tree
// mapping - the node shape is identical between a schema version and a component version
// (Step 4 §5: "no new concepts"), so the mapping is too.
public static class ComponentVersionsMappingExtensions
{
    public static CreateComponentVersionCommand ToCommand(this CreateComponentVersionRequest request, Guid componentDefinitionId) =>
        new(componentDefinitionId, request.BumpKind.ToDomain(), request.ChangeSummary);

    public static CreateComponentVersionResponse ToResponse(this CreateComponentVersionResult result) =>
        new(result.ComponentVersionId, result.VersionNumber.ToString());

    public static ComponentVersionSummaryResponse ToResponse(this ComponentVersionSummary summary) => new(
        summary.Id, summary.VersionNumber.ToString(), summary.Status.ToContract(), summary.ChangeSummary, summary.PublishedAt);

    public static ComponentVersionDetailResponse ToResponse(this ComponentVersionDetail detail) => new(
        detail.Id, detail.ComponentDefinitionId, detail.VersionNumber.ToString(), detail.Status.ToContract(),
        detail.ChangeSummary, detail.PublishedAt, detail.RootNode.ToResponse(),
        [.. detail.LocalDefinitions.Select(d => d.ToResponse())]);

    public static AddComponentNodeCommand ToCommand(this AddComponentNodeRequest request, Guid componentVersionId) =>
        new(componentVersionId, request.ParentNodeId, request.AttachmentKind.ToDomain(), request.PropertyName, request.Kind?.ToDomain());

    public static AddComponentNodeResponse ToResponse(this AddComponentNodeResult result) => new(result.NodeId);

    // Named distinctly (not an overloaded ToCommand) - MoveSchemaNodeRequest is reused as-is for
    // component node moves too (identical shape), but two extension methods differing only by
    // return type on the same extended type is an unresolvable overload for the compiler.
    public static MoveComponentNodeCommand ToMoveComponentNodeCommand(
        this MoveSchemaNodeRequest request, Guid componentVersionId, Guid nodeId) =>
        new(componentVersionId, nodeId, request.NewOrder);

    public static ReparentComponentNodeCommand ToReparentComponentNodeCommand(
        this MoveSchemaNodeRequest request, Guid componentVersionId, Guid nodeId, Guid newParentNodeId) =>
        new(componentVersionId, nodeId, newParentNodeId, request.AttachmentKind?.ToDomain(), request.PropertyName);

    // Named distinctly for the same reason as ToMoveComponentNodeCommand above -
    // AddLocalDefinitionRequest is shared verbatim with SchemaVersionsController.
    public static AddComponentLocalDefinitionCommand ToAddComponentLocalDefinitionCommand(
        this AddLocalDefinitionRequest request, Guid componentVersionId) =>
        new(componentVersionId, request.Name, request.RootKind?.ToDomain());

    public static AddLocalDefinitionResponse ToResponse(this AddComponentLocalDefinitionResult result) =>
        new(result.LocalDefinitionId);
}
