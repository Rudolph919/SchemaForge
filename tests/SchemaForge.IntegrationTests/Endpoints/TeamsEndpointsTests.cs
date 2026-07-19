using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SchemaForge.Contracts.V1.Auth;
using SchemaForge.Contracts.V1.Organizations;
using SchemaForge.Contracts.V1.Teams;
using SchemaForge.IntegrationTests.Fixtures;

namespace SchemaForge.IntegrationTests.Endpoints;

[Collection(nameof(IntegrationTestCollection))]
public sealed class TeamsEndpointsTests : IAsyncLifetime
{
    private SchemaForgeApiFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new SchemaForgeApiFactory();
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Create_then_add_member_is_reflected_in_get_and_list()
    {
        var (owner, ownerToken) = await RegisterAndLoginAsync("Org A");
        var (member, _) = await RegisterAndLoginAsync("Org B");
        await InviteAndAcceptAsync(ownerToken, member.Email, member.Token);

        var createResponse = await SendAsync(HttpMethod.Post, "/api/v1/teams", ownerToken,
            new CreateTeamRequest("Platform Team", "Core platform"));
        var team = await createResponse.Content.ReadFromJsonAsync<CreateTeamResponse>();

        var addResponse = await SendAsync(HttpMethod.Post, $"/api/v1/teams/{team!.TeamId}/members", ownerToken,
            new AddTeamMemberRequest(member.UserId));
        addResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detailResponse = await SendAsync(HttpMethod.Get, $"/api/v1/teams/{team.TeamId}", ownerToken);
        var detail = await detailResponse.Content.ReadFromJsonAsync<TeamDetailResponse>();
        detail!.Members.Should().ContainSingle(m => m.UserId == member.UserId);

        var listResponse = await SendAsync(HttpMethod.Get, "/api/v1/teams", ownerToken);
        var list = await listResponse.Content.ReadFromJsonAsync<List<TeamSummaryResponse>>();
        list.Should().ContainSingle(t => t.Id == team.TeamId && t.MemberCount == 1);
    }

    [Fact]
    public async Task Adding_a_non_organization_member_to_a_team_is_rejected()
    {
        var (_, ownerToken) = await RegisterAndLoginAsync("Org A");
        var (outsider, _) = await RegisterAndLoginAsync("Org B");

        var createResponse = await SendAsync(HttpMethod.Post, "/api/v1/teams", ownerToken,
            new CreateTeamRequest("Platform Team", null));
        var team = await createResponse.Content.ReadFromJsonAsync<CreateTeamResponse>();

        var addResponse = await SendAsync(HttpMethod.Post, $"/api/v1/teams/{team!.TeamId}/members", ownerToken,
            new AddTeamMemberRequest(outsider.UserId));

        addResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Removing_a_member_drops_them_from_the_team_but_not_the_organization()
    {
        var (_, ownerToken) = await RegisterAndLoginAsync("Org A");
        var (member, _) = await RegisterAndLoginAsync("Org B");
        await InviteAndAcceptAsync(ownerToken, member.Email, member.Token);

        var createResponse = await SendAsync(HttpMethod.Post, "/api/v1/teams", ownerToken,
            new CreateTeamRequest("Platform Team", null));
        var team = await createResponse.Content.ReadFromJsonAsync<CreateTeamResponse>();

        await SendAsync(HttpMethod.Post, $"/api/v1/teams/{team!.TeamId}/members", ownerToken,
            new AddTeamMemberRequest(member.UserId));

        var removeResponse = await SendAsync(
            HttpMethod.Delete, $"/api/v1/teams/{team.TeamId}/members/{member.UserId}", ownerToken);
        removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detailResponse = await SendAsync(HttpMethod.Get, $"/api/v1/teams/{team.TeamId}", ownerToken);
        var detail = await detailResponse.Content.ReadFromJsonAsync<TeamDetailResponse>();
        detail!.Members.Should().BeEmpty();

        var membershipsResponse = await SendAsync(HttpMethod.Get, "/api/v1/organizations/members", ownerToken);
        var members = await membershipsResponse.Content.ReadFromJsonAsync<List<OrganizationMemberResponse>>(TestJson.Options);
        members.Should().ContainSingle(m => m.UserId == member.UserId && m.Status == MembershipStatus.Active);
    }

    private async Task InviteAndAcceptAsync(string inviterToken, string inviteeEmail, string inviteeToken)
    {
        var inviteResponse = await SendAsync(HttpMethod.Post, "/api/v1/organizations/members/invite", inviterToken,
            new InviteMemberRequest(inviteeEmail, OrganizationRole.Member));
        var invite = await inviteResponse.Content.ReadFromJsonAsync<InviteMemberResponse>();

        await SendAsync(HttpMethod.Post, $"/api/v1/organizations/members/{invite!.MembershipId}/accept", inviteeToken);
    }

    private async Task<(RegisteredUser User, string Token)> RegisterAndLoginAsync(string organizationName)
    {
        var email = $"{Guid.NewGuid()}@example.com";
        const string password = "correct-horse-battery";

        var registerResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/register", new RegisterRequest(email, password, "Test User", organizationName));
        var registration = (await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>())!;

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
        var login = (await loginResponse.Content.ReadFromJsonAsync<LoginResponse>())!;

        var user = new RegisteredUser(registration.UserId, registration.OrganizationId, email, login.AccessToken);
        return (user, login.AccessToken);
    }

    private sealed record RegisteredUser(Guid UserId, Guid OrganizationId, string Email, string Token);

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string token, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await _client.SendAsync(request);
    }
}
