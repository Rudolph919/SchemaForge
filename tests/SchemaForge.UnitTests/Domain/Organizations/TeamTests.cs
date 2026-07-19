using FluentAssertions;
using SchemaForge.Domain.Organizations;

namespace SchemaForge.UnitTests.Domain.Organizations;

public class TeamTests
{
    [Fact]
    public void Create_raises_a_TeamCreated_domain_event()
    {
        var organizationId = Guid.NewGuid();

        var team = Team.Create(organizationId, "Platform");

        team.OrganizationId.Should().Be(organizationId);
        team.Name.Should().Be("Platform");
        team.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<TeamCreated>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_a_blank_name(string name)
    {
        var act = () => Team.Create(Guid.NewGuid(), name);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Rename_updates_the_name()
    {
        var team = Team.Create(Guid.NewGuid(), "Platform");

        var result = team.Rename("Infrastructure");

        result.IsSuccess.Should().BeTrue();
        team.Name.Should().Be("Infrastructure");
    }

    [Fact]
    public void Rename_rejects_a_blank_name()
    {
        var team = Team.Create(Guid.NewGuid(), "Platform");

        var result = team.Rename("  ");

        result.IsFailure.Should().BeTrue();
        team.Name.Should().Be("Platform");
    }

    [Fact]
    public void AddMember_adds_a_new_member_and_raises_an_event()
    {
        var team = Team.Create(Guid.NewGuid(), "Platform");
        var userId = Guid.NewGuid();

        var result = team.AddMember(userId);

        result.IsSuccess.Should().BeTrue();
        team.Members.Should().ContainSingle(m => m.UserId == userId);
        team.DomainEvents.Should().Contain(e => e is TeamMemberAdded);
    }

    [Fact]
    public void AddMember_rejects_a_duplicate_member()
    {
        var team = Team.Create(Guid.NewGuid(), "Platform");
        var userId = Guid.NewGuid();
        team.AddMember(userId);

        var result = team.AddMember(userId);

        result.IsFailure.Should().BeTrue();
        team.Members.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveMember_removes_an_existing_member_and_raises_an_event()
    {
        var team = Team.Create(Guid.NewGuid(), "Platform");
        var userId = Guid.NewGuid();
        team.AddMember(userId);

        var result = team.RemoveMember(userId);

        result.IsSuccess.Should().BeTrue();
        team.Members.Should().BeEmpty();
        team.DomainEvents.Should().Contain(e => e is TeamMemberRemoved);
    }

    [Fact]
    public void RemoveMember_fails_for_a_user_who_is_not_a_member()
    {
        var team = Team.Create(Guid.NewGuid(), "Platform");

        var result = team.RemoveMember(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
    }
}
