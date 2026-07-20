using System.Text.Json;
using SchemaForge.Application.Schemas;
using SchemaForge.Application.Schemas.Commands.AddSchemaNode;
using SchemaForge.Application.Schemas.Commands.CreateSchemaVersion;
using SchemaForge.Application.Schemas.Commands.MoveSchemaNode;
using SchemaForge.Application.Schemas.Queries.GetSchemaVersion;
using SchemaForge.Contracts.V1.Schemas;
using SchemaForge.Domain.Schemas;
using SchemaForge.Domain.Schemas.ValueObjects;
using DomainCompositionKind = SchemaForge.Domain.Schemas.CompositionKind;
using DomainNodeAttachmentKind = SchemaForge.Application.Schemas.NodeAttachmentKind;
using DomainNodeKind = SchemaForge.Domain.Schemas.NodeKind;
using DomainSchemaFormat = SchemaForge.Domain.Schemas.SchemaFormat;
using DomainSchemaLifecycleStatus = SchemaForge.Domain.Schemas.SchemaLifecycleStatus;
using DomainVersionBumpKind = SchemaForge.Application.Schemas.VersionBumpKind;
using DomainVersionConstraintKind = SchemaForge.Domain.Schemas.VersionConstraintKind;
using ContractCompositionKind = SchemaForge.Contracts.V1.Schemas.CompositionKind;
using ContractNodeAttachmentKind = SchemaForge.Contracts.V1.Schemas.NodeAttachmentKind;
using ContractNodeKind = SchemaForge.Contracts.V1.Schemas.NodeKind;
using ContractSchemaFormat = SchemaForge.Contracts.V1.Schemas.SchemaFormat;
using ContractSchemaLifecycleStatus = SchemaForge.Contracts.V1.Schemas.SchemaLifecycleStatus;
using ContractVersionBumpKind = SchemaForge.Contracts.V1.Schemas.VersionBumpKind;
using ContractVersionConstraintKind = SchemaForge.Contracts.V1.Schemas.VersionConstraintKind;

namespace SchemaForge.Api.Mapping;

public static class SchemaVersionsMappingExtensions
{
    public static CreateSchemaVersionCommand ToCommand(this CreateSchemaVersionRequest request, Guid schemaDefinitionId) =>
        new(schemaDefinitionId, request.BumpKind.ToDomain(), request.ChangeSummary);

    public static CreateSchemaVersionResponse ToResponse(this CreateSchemaVersionResult result) =>
        new(result.SchemaVersionId, result.VersionNumber.ToString());

    public static SchemaVersionSummaryResponse ToResponse(this SchemaVersionSummary summary) => new(
        summary.Id, summary.VersionNumber.ToString(), summary.Status.ToContract(), summary.ChangeSummary, summary.PublishedAt);

    public static SchemaVersionDetailResponse ToResponse(this SchemaVersionDetail detail) => new(
        detail.Id, detail.SchemaDefinitionId, detail.VersionNumber.ToString(), detail.Status.ToContract(),
        detail.ChangeSummary, detail.PublishedAt, detail.RootNode.ToResponse(),
        [.. detail.LocalDefinitions.Select(d => d.ToResponse())]);

    public static AddSchemaNodeCommand ToCommand(this AddSchemaNodeRequest request, Guid schemaVersionId) =>
        new(schemaVersionId, request.ParentNodeId, request.AttachmentKind.ToDomain(), request.PropertyName, request.Kind?.ToDomain());

    public static AddSchemaNodeResponse ToResponse(this AddSchemaNodeResult result) => new(result.NodeId);

    public static MoveSchemaNodeCommand ToCommand(this MoveSchemaNodeRequest request, Guid schemaVersionId, Guid nodeId) =>
        new(schemaVersionId, nodeId, request.NewOrder);

    public static SchemaNodeContent ToDomain(this UpdateSchemaNodeRequest request) => new(
        request.Kind?.ToDomain(),
        request.Description,
        request.Notes,
        request.IsNullable,
        request.IsRequiredByParent,
        [.. request.Examples.Select(ToJsonLiteral)],
        request.DefaultValue is { } defaultValue ? ToJsonLiteral(defaultValue) : null,
        request.AllowedValues?.Select(ToJsonLiteral).ToList(),
        request.ConstValue is { } constValue ? ToJsonLiteral(constValue) : null,
        request.ObjectConstraints?.ToDomain(),
        request.ArrayConstraints?.ToDomain(),
        request.StringConstraints?.ToDomain(),
        request.NumericConstraints?.ToDomain(),
        request.DependentRequired,
        request.Composition?.ToDomain(),
        request.ComponentReference?.ToDomain(),
        request.LocalDefinitionRef);

    internal static SchemaNodeResponse ToResponse(this SchemaNode node) => new(
        node.Id,
        node.PropertyName,
        node.Order,
        node.Kind?.ToContract(),
        node.Description,
        node.Notes,
        node.IsNullable,
        node.IsRequiredByParent,
        [.. node.Examples.Select(e => e.AsJsonElement())],
        node.DefaultValue?.AsJsonElement(),
        node.AllowedValues?.Select(e => e.AsJsonElement()).ToList(),
        node.ConstValue?.AsJsonElement(),
        node.ObjectConstraints?.ToResponse(),
        node.ArrayConstraints?.ToResponse(),
        node.StringConstraints?.ToResponse(),
        node.NumericConstraints?.ToResponse(),
        [.. node.Properties.Select(n => n.ToResponse())],
        [.. node.PrefixItems.Select(n => n.ToResponse())],
        node.ItemsNode?.ToResponse(),
        node.DependentRequired,
        node.Composition?.ToContract(),
        [.. node.CompositionBranches.Select(n => n.ToResponse())],
        node.IfNode?.ToResponse(),
        node.ThenNode?.ToResponse(),
        node.ElseNode?.ToResponse(),
        node.ComponentReference?.ToResponse(),
        node.LocalDefinitionRef);

    internal static LocalDefinitionResponse ToResponse(this LocalDefinition definition) =>
        new(definition.Id, definition.Name, definition.RootNode.ToResponse());

    internal static ObjectConstraintsDto ToResponse(this ObjectConstraints c) =>
        new(c.MinProperties, c.MaxProperties, c.AdditionalPropertiesAllowed);

    internal static ObjectConstraints ToDomain(this ObjectConstraintsDto c) =>
        new(c.MinProperties, c.MaxProperties, c.AdditionalPropertiesAllowed);

    internal static ArrayConstraintsDto ToResponse(this ArrayConstraints c) => new(c.MinItems, c.MaxItems, c.UniqueItems);

    internal static ArrayConstraints ToDomain(this ArrayConstraintsDto c) => new(c.MinItems, c.MaxItems, c.UniqueItems);

    internal static StringConstraintsDto ToResponse(this StringConstraints c) =>
        new(c.MinLength, c.MaxLength, c.Pattern, c.Format?.ToContract(), c.CustomFormatValue);

    internal static StringConstraints ToDomain(this StringConstraintsDto c) =>
        new(c.MinLength, c.MaxLength, c.Pattern, c.Format?.ToDomain(), c.CustomFormatValue);

    internal static NumericConstraintsDto ToResponse(this NumericConstraints c) =>
        new(c.Minimum, c.Maximum, c.ExclusiveMinimum, c.ExclusiveMaximum, c.MultipleOf);

    internal static NumericConstraints ToDomain(this NumericConstraintsDto c) =>
        new(c.Minimum, c.Maximum, c.ExclusiveMinimum, c.ExclusiveMaximum, c.MultipleOf);

    internal static ComponentReferenceDto ToResponse(this ComponentReference reference) => new(
        reference.ComponentVersionId,
        new VersionConstraintDto(reference.Constraint.Kind.ToContract(), reference.Constraint.Version?.ToString()));

    internal static ComponentReference ToDomain(this ComponentReferenceDto dto)
    {
        var constraint = dto.Constraint.Kind switch
        {
            ContractVersionConstraintKind.ExactVersion => VersionConstraint.ExactVersion(ParseSemVer(dto.Constraint.Version!)),
            ContractVersionConstraintKind.MinimumVersion => VersionConstraint.MinimumVersion(ParseSemVer(dto.Constraint.Version!)),
            ContractVersionConstraintKind.Latest => VersionConstraint.Latest,
            _ => throw new ArgumentOutOfRangeException(nameof(dto), dto.Constraint.Kind, "Unknown version constraint kind."),
        };

        return new ComponentReference(dto.ComponentVersionId, constraint);
    }

    internal static SharedKernel.Primitives.SemVer ParseSemVer(string value)
    {
        var parts = value.Split('.');
        return SharedKernel.Primitives.SemVer.Create(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
    }

    internal static JsonLiteral ToJsonLiteral(JsonElement element) => JsonLiteral.FromRawJson(element.GetRawText());

    internal static DomainVersionBumpKind ToDomain(this ContractVersionBumpKind kind) => kind switch
    {
        ContractVersionBumpKind.Major => DomainVersionBumpKind.Major,
        ContractVersionBumpKind.Minor => DomainVersionBumpKind.Minor,
        ContractVersionBumpKind.Patch => DomainVersionBumpKind.Patch,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown version bump kind."),
    };

    internal static ContractSchemaLifecycleStatus ToContract(this DomainSchemaLifecycleStatus status) => status switch
    {
        DomainSchemaLifecycleStatus.Draft => ContractSchemaLifecycleStatus.Draft,
        DomainSchemaLifecycleStatus.Published => ContractSchemaLifecycleStatus.Published,
        DomainSchemaLifecycleStatus.Deprecated => ContractSchemaLifecycleStatus.Deprecated,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown schema lifecycle status."),
    };

    internal static DomainNodeKind ToDomain(this ContractNodeKind kind) => kind switch
    {
        ContractNodeKind.Object => DomainNodeKind.Object,
        ContractNodeKind.Array => DomainNodeKind.Array,
        ContractNodeKind.String => DomainNodeKind.String,
        ContractNodeKind.Number => DomainNodeKind.Number,
        ContractNodeKind.Integer => DomainNodeKind.Integer,
        ContractNodeKind.Boolean => DomainNodeKind.Boolean,
        ContractNodeKind.Null => DomainNodeKind.Null,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown node kind."),
    };

    internal static ContractNodeKind ToContract(this DomainNodeKind kind) => kind switch
    {
        DomainNodeKind.Object => ContractNodeKind.Object,
        DomainNodeKind.Array => ContractNodeKind.Array,
        DomainNodeKind.String => ContractNodeKind.String,
        DomainNodeKind.Number => ContractNodeKind.Number,
        DomainNodeKind.Integer => ContractNodeKind.Integer,
        DomainNodeKind.Boolean => ContractNodeKind.Boolean,
        DomainNodeKind.Null => ContractNodeKind.Null,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown node kind."),
    };

    internal static DomainCompositionKind ToDomain(this ContractCompositionKind kind) => kind switch
    {
        ContractCompositionKind.OneOf => DomainCompositionKind.OneOf,
        ContractCompositionKind.AnyOf => DomainCompositionKind.AnyOf,
        ContractCompositionKind.AllOf => DomainCompositionKind.AllOf,
        ContractCompositionKind.Not => DomainCompositionKind.Not,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown composition kind."),
    };

    internal static ContractCompositionKind ToContract(this DomainCompositionKind kind) => kind switch
    {
        DomainCompositionKind.OneOf => ContractCompositionKind.OneOf,
        DomainCompositionKind.AnyOf => ContractCompositionKind.AnyOf,
        DomainCompositionKind.AllOf => ContractCompositionKind.AllOf,
        DomainCompositionKind.Not => ContractCompositionKind.Not,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown composition kind."),
    };

    internal static DomainSchemaFormat ToDomain(this ContractSchemaFormat format) => format switch
    {
        ContractSchemaFormat.Date => DomainSchemaFormat.Date,
        ContractSchemaFormat.DateTime => DomainSchemaFormat.DateTime,
        ContractSchemaFormat.Time => DomainSchemaFormat.Time,
        ContractSchemaFormat.Email => DomainSchemaFormat.Email,
        ContractSchemaFormat.Hostname => DomainSchemaFormat.Hostname,
        ContractSchemaFormat.Ipv4 => DomainSchemaFormat.Ipv4,
        ContractSchemaFormat.Ipv6 => DomainSchemaFormat.Ipv6,
        ContractSchemaFormat.Uri => DomainSchemaFormat.Uri,
        ContractSchemaFormat.UriReference => DomainSchemaFormat.UriReference,
        ContractSchemaFormat.Uuid => DomainSchemaFormat.Uuid,
        ContractSchemaFormat.Custom => DomainSchemaFormat.Custom,
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown schema format."),
    };

    internal static ContractSchemaFormat ToContract(this DomainSchemaFormat format) => format switch
    {
        DomainSchemaFormat.Date => ContractSchemaFormat.Date,
        DomainSchemaFormat.DateTime => ContractSchemaFormat.DateTime,
        DomainSchemaFormat.Time => ContractSchemaFormat.Time,
        DomainSchemaFormat.Email => ContractSchemaFormat.Email,
        DomainSchemaFormat.Hostname => ContractSchemaFormat.Hostname,
        DomainSchemaFormat.Ipv4 => ContractSchemaFormat.Ipv4,
        DomainSchemaFormat.Ipv6 => ContractSchemaFormat.Ipv6,
        DomainSchemaFormat.Uri => ContractSchemaFormat.Uri,
        DomainSchemaFormat.UriReference => ContractSchemaFormat.UriReference,
        DomainSchemaFormat.Uuid => ContractSchemaFormat.Uuid,
        DomainSchemaFormat.Custom => ContractSchemaFormat.Custom,
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown schema format."),
    };

    internal static ContractVersionConstraintKind ToContract(this DomainVersionConstraintKind kind) => kind switch
    {
        DomainVersionConstraintKind.ExactVersion => ContractVersionConstraintKind.ExactVersion,
        DomainVersionConstraintKind.MinimumVersion => ContractVersionConstraintKind.MinimumVersion,
        DomainVersionConstraintKind.Latest => ContractVersionConstraintKind.Latest,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown version constraint kind."),
    };

    internal static DomainNodeAttachmentKind ToDomain(this ContractNodeAttachmentKind kind) => kind switch
    {
        ContractNodeAttachmentKind.ObjectProperty => DomainNodeAttachmentKind.ObjectProperty,
        ContractNodeAttachmentKind.ArrayPrefixItem => DomainNodeAttachmentKind.ArrayPrefixItem,
        ContractNodeAttachmentKind.ArrayItems => DomainNodeAttachmentKind.ArrayItems,
        ContractNodeAttachmentKind.CompositionBranch => DomainNodeAttachmentKind.CompositionBranch,
        ContractNodeAttachmentKind.ConditionalIf => DomainNodeAttachmentKind.ConditionalIf,
        ContractNodeAttachmentKind.ConditionalThen => DomainNodeAttachmentKind.ConditionalThen,
        ContractNodeAttachmentKind.ConditionalElse => DomainNodeAttachmentKind.ConditionalElse,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown node attachment kind."),
    };
}
