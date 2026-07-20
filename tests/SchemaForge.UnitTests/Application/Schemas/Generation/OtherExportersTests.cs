using System.Text.Json;
using FluentAssertions;
using SchemaForge.Application.Schemas.Generation;
using SchemaForge.Domain.Schemas;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.UnitTests.Application.Schemas.Generation;

// Lighter-weight than JsonSchemaExporterTests - these three formats are "good enough MVP"
// converters by design (composition/negation don't map cleanly onto TS unions or C# records), so
// the bar here is "produces sensible, non-throwing output for the common cases," not full fidelity.
public class OtherExportersTests
{
    private static SchemaVersion SchemaWithOneRequiredStringProperty()
    {
        var version = SchemaVersion.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), SemVer.Initial);
        var nodeId = version.AddObjectProperty(version.RootNode.Id, "invoiceNumber", NodeKind.String).Value;
        version.UpdateNode(nodeId, SchemaNodeContent.Empty(NodeKind.String) with { IsRequiredByParent = true });
        return version;
    }

    [Fact]
    public async Task OpenApiExporter_wraps_the_schema_under_components_schemas()
    {
        var version = SchemaWithOneRequiredStringProperty();

        var json = await new OpenApiExporter().ExportAsync(version, CancellationToken.None);
        var root = JsonDocument.Parse(json).RootElement;

        root.GetProperty("openapi").GetString().Should().StartWith("3.1");
        root.GetProperty("components").GetProperty("schemas").GetProperty("Schema")
            .GetProperty("properties").GetProperty("invoiceNumber").GetProperty("type").GetString().Should().Be("string");
    }

    [Fact]
    public async Task TypeScriptExporter_produces_an_interface_with_a_required_property()
    {
        var version = SchemaWithOneRequiredStringProperty();

        var typescript = await new TypeScriptExporter().ExportAsync(version, CancellationToken.None);

        typescript.Should().Contain("export type Schema");
        typescript.Should().Contain("invoiceNumber: string");
        typescript.Should().NotContain("invoiceNumber?:");
    }

    [Fact]
    public async Task CSharpExporter_produces_a_record_with_the_property()
    {
        var version = SchemaWithOneRequiredStringProperty();

        var csharp = await new CSharpExporter().ExportAsync(version, CancellationToken.None);

        csharp.Should().Contain("public sealed record Schema(");
        csharp.Should().Contain("string InvoiceNumber");
    }

    [Fact]
    public async Task CSharpExporter_hoists_a_named_record_per_nested_object()
    {
        var version = SchemaVersion.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), SemVer.Initial);
        var addressId = version.AddObjectProperty(version.RootNode.Id, "billingAddress", NodeKind.Object).Value;
        version.AddObjectProperty(addressId, "street", NodeKind.String);

        var csharp = await new CSharpExporter().ExportAsync(version, CancellationToken.None);

        csharp.Should().Contain("public sealed record BillingAddress(");
        csharp.Should().Contain("BillingAddress? BillingAddress");
    }
}
