using FluentAssertions;
using SchemaForge.Domain.Organizations;
using SchemaForge.SharedKernel.Primitives;

namespace SchemaForge.UnitTests.Domain.Organizations;

public class OrganizationTests
{
    [Fact]
    public void Create_defaults_to_active_status_and_free_plan()
    {
        var organization = Organization.Create("Acme Corp", Slug.Create("acme-corp"));

        organization.Status.Should().Be(OrganizationStatus.Active);
        organization.PlanTier.Should().Be(PlanTier.Free);
    }

    [Fact]
    public void Create_raises_an_OrganizationCreated_domain_event()
    {
        var organization = Organization.Create("Acme Corp", Slug.Create("acme-corp"));

        organization.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrganizationCreated>()
            .Which.Name.Should().Be("Acme Corp");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_a_blank_name(string name)
    {
        var act = () => Organization.Create(name, Slug.Create("acme-corp"));

        act.Should().Throw<ArgumentException>();
    }
}
