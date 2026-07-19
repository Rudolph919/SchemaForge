using FluentAssertions;
using SchemaForge.Domain.Workspaces;

namespace SchemaForge.UnitTests.Domain.Workspaces;

public class ProjectTests
{
    [Fact]
    public void Create_defaults_to_active_status_and_raises_an_event()
    {
        var organizationId = Guid.NewGuid();

        var project = Project.Create(organizationId, "Accounts Payable");

        project.OrganizationId.Should().Be(organizationId);
        project.Status.Should().Be(ProjectStatus.Active);
        project.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<ProjectCreated>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_a_blank_name(string name)
    {
        var act = () => Project.Create(Guid.NewGuid(), name);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Archive_transitions_to_archived_and_raises_an_event()
    {
        var project = Project.Create(Guid.NewGuid(), "Accounts Payable");

        var result = project.Archive();

        result.IsSuccess.Should().BeTrue();
        project.Status.Should().Be(ProjectStatus.Archived);
        project.DomainEvents.Should().Contain(e => e is ProjectArchived);
    }

    [Fact]
    public void Archive_fails_for_an_already_archived_project()
    {
        var project = Project.Create(Guid.NewGuid(), "Accounts Payable");
        project.Archive();

        var result = project.Archive();

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Reactivate_transitions_an_archived_project_back_to_active()
    {
        var project = Project.Create(Guid.NewGuid(), "Accounts Payable");
        project.Archive();

        var result = project.Reactivate();

        result.IsSuccess.Should().BeTrue();
        project.Status.Should().Be(ProjectStatus.Active);
        project.DomainEvents.Should().Contain(e => e is ProjectReactivated);
    }

    [Fact]
    public void Reactivate_fails_for_an_already_active_project()
    {
        var project = Project.Create(Guid.NewGuid(), "Accounts Payable");

        var result = project.Reactivate();

        result.IsFailure.Should().BeTrue();
    }
}
