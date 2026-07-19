using FluentAssertions;
using SchemaForge.Domain.Schemas;

namespace SchemaForge.UnitTests.Domain.Schemas;

public class SchemaDefinitionTests
{
    [Fact]
    public void Create_sets_fields_and_raises_an_event()
    {
        var organizationId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        var definition = SchemaDefinition.Create(organizationId, projectId, "Invoice Schema", "Vendor invoices");

        definition.OrganizationId.Should().Be(organizationId);
        definition.ProjectId.Should().Be(projectId);
        definition.Name.Should().Be("Invoice Schema");
        definition.Description.Should().Be("Vendor invoices");
        definition.Tags.Should().BeEmpty();
        definition.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<SchemaDefinitionCreated>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_a_blank_name(string name)
    {
        var act = () => SchemaDefinition.Create(Guid.NewGuid(), Guid.NewGuid(), name);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Rename_updates_the_name()
    {
        var definition = SchemaDefinition.Create(Guid.NewGuid(), Guid.NewGuid(), "Invoice Schema");

        var result = definition.Rename("Vendor Invoice Schema");

        result.IsSuccess.Should().BeTrue();
        definition.Name.Should().Be("Vendor Invoice Schema");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_rejects_a_blank_name(string newName)
    {
        var definition = SchemaDefinition.Create(Guid.NewGuid(), Guid.NewGuid(), "Invoice Schema");

        var result = definition.Rename(newName);

        result.IsFailure.Should().BeTrue();
        definition.Name.Should().Be("Invoice Schema");
    }

    [Fact]
    public void UpdateTags_replaces_the_tag_list()
    {
        var definition = SchemaDefinition.Create(Guid.NewGuid(), Guid.NewGuid(), "Invoice Schema");

        definition.UpdateTags(["finance", "vendor"]);

        definition.Tags.Should().BeEquivalentTo(["finance", "vendor"]);
    }
}
