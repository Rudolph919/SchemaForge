using FluentAssertions;
using SchemaForge.Domain.Components;
using SchemaForge.Domain.Components.Events;

namespace SchemaForge.UnitTests.Domain.Components;

public class ComponentDefinitionTests
{
    [Fact]
    public void Create_sets_fields_and_raises_an_event()
    {
        var organizationId = Guid.NewGuid();

        var definition = ComponentDefinition.Create(organizationId, "PostalAddress", "Reusable address shape");

        definition.OrganizationId.Should().Be(organizationId);
        definition.Name.Should().Be("PostalAddress");
        definition.Description.Should().Be("Reusable address shape");
        definition.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<ComponentDefinitionCreated>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_a_blank_name(string name)
    {
        var act = () => ComponentDefinition.Create(Guid.NewGuid(), name);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Rename_updates_the_name()
    {
        var definition = ComponentDefinition.Create(Guid.NewGuid(), "PostalAddress");

        var result = definition.Rename("MailingAddress");

        result.IsSuccess.Should().BeTrue();
        definition.Name.Should().Be("MailingAddress");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_rejects_a_blank_name(string newName)
    {
        var definition = ComponentDefinition.Create(Guid.NewGuid(), "PostalAddress");

        var result = definition.Rename(newName);

        result.IsFailure.Should().BeTrue();
        definition.Name.Should().Be("PostalAddress");
    }

    [Fact]
    public void UpdateDescription_replaces_the_description()
    {
        var definition = ComponentDefinition.Create(Guid.NewGuid(), "PostalAddress");

        definition.UpdateDescription("Updated description");

        definition.Description.Should().Be("Updated description");
    }
}
