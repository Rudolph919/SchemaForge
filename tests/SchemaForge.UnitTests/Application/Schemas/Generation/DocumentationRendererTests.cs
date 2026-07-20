using System.Text.Json;
using FluentAssertions;
using SchemaForge.Application.Schemas.Generation;
using SchemaForge.Domain.Schemas;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.UnitTests.Application.Schemas.Generation;

public class DocumentationRendererTests
{
    private static SchemaVersion SchemaWithARequiredDescribedProperty()
    {
        var version = SchemaVersion.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), SemVer.Initial);
        var nodeId = version.AddObjectProperty(version.RootNode.Id, "invoiceNumber", NodeKind.String).Value;
        version.UpdateNode(nodeId, SchemaNodeContent.Empty(NodeKind.String) with
        {
            IsRequiredByParent = true,
            Description = "The invoice's unique number",
        });
        return version;
    }

    [Fact]
    public async Task Json_renderer_includes_the_field_and_its_description()
    {
        var json = await new JsonDocumentationRenderer().RenderAsync(SchemaWithARequiredDescribedProperty(), CancellationToken.None);

        // System.Text.Json's default encoder escapes apostrophes as ' (valid JSON, just not
        // literal-substring-matchable) - parse and check the actual value instead of the raw text.
        // fields[0] is the root node itself (PropertyName null); the property is fields[1].
        var field = JsonDocument.Parse(json).RootElement.GetProperty("fields")[1];
        field.GetProperty("propertyName").GetString().Should().Be("invoiceNumber");
        field.GetProperty("description").GetString().Should().Be("The invoice's unique number");
        field.GetProperty("required").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Markdown_renderer_includes_the_field_and_its_description()
    {
        var markdown = await new MarkdownDocumentationRenderer().RenderAsync(SchemaWithARequiredDescribedProperty(), CancellationToken.None);

        markdown.Should().Contain("**invoiceNumber**");
        markdown.Should().Contain("required");
        markdown.Should().Contain("The invoice's unique number");
    }

    [Fact]
    public async Task Html_renderer_escapes_a_description_containing_markup()
    {
        var version = SchemaVersion.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), SemVer.Initial);
        var nodeId = version.AddObjectProperty(version.RootNode.Id, "note", NodeKind.String).Value;
        version.UpdateNode(nodeId, SchemaNodeContent.Empty(NodeKind.String) with
        {
            Description = "<script>alert(1)</script>",
        });

        var html = await new HtmlDocumentationRenderer().RenderAsync(version, CancellationToken.None);

        html.Should().NotContain("<script>alert(1)</script>");
        html.Should().Contain("&lt;script&gt;");
    }
}
