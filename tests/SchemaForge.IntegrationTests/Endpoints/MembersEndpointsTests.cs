using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SchemaForge.Contracts.V1.Auth;
using SchemaForge.Contracts.V1.Organizations;
using SchemaForge.IntegrationTests.Fixtures;

namespace SchemaForge.IntegrationTests.Endpoints;

[Collection(nameof(IntegrationTestCollection))]
public sealed class MembersEndpointsTests : IAsyncLifetime
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
    public async Task Invite_then_accept_makes_the_invitee_an_active_member_of_the_inviting_org()
    {
        var (ownerA, tokenA) = await RegisterAndLoginAsync("Org A");
        var (ownerB, tokenB) = await RegisterAndLoginAsync("Org B");

        var inviteResponse = await SendAsync(HttpMethod.Post, "/api/v1/organizations/members/invite", tokenA,
            new InviteMemberRequest(ownerB.Email, OrganizationRole.Member));
        inviteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var invite = await inviteResponse.Content.ReadFromJsonAsync<InviteMemberResponse>();

        var acceptResponse = await SendAsync(
            HttpMethod.Post, $"/api/v1/organizations/members/{invite!.MembershipId}/accept", tokenB);
        acceptResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var membersResponse = await SendAsync(HttpMethod.Get, "/api/v1/organizations/members", tokenA);
        var members = await membersResponse.Content.ReadFromJsonAsync<List<OrganizationMemberResponse>>(TestJson.Options);

        members.Should().ContainSingle(
            m => m.MembershipId == invite.MembershipId
                && m.Status == MembershipStatus.Active
                && m.Role == OrganizationRole.Member);
    }

    [Fact]
    public async Task A_user_cannot_accept_an_invitation_addressed_to_someone_else()
    {
        var (ownerA, tokenA) = await RegisterAndLoginAsync("Org A");
        var (ownerB, _) = await RegisterAndLoginAsync("Org B");
        var (_, tokenC) = await RegisterAndLoginAsync("Org C");

        var inviteResponse = await SendAsync(HttpMethod.Post, "/api/v1/organizations/members/invite", tokenA,
            new InviteMemberRequest(ownerB.Email, OrganizationRole.Member));
        var invite = await inviteResponse.Content.ReadFromJsonAsync<InviteMemberResponse>();

        var acceptAsWrongUser = await SendAsync(
            HttpMethod.Post, $"/api/v1/organizations/members/{invite!.MembershipId}/accept", tokenC);

        acceptAsWrongUser.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Revoking_the_organizations_only_owner_is_rejected()
    {
        var (_, tokenA) = await RegisterAndLoginAsync("Org A");

        var membersResponse = await SendAsync(HttpMethod.Get, "/api/v1/organizations/members", tokenA);
        var members = await membersResponse.Content.ReadFromJsonAsync<List<OrganizationMemberResponse>>(TestJson.Options);
        var ownerMembershipId = members!.Single().MembershipId;

        var revokeResponse = await SendAsync(
            HttpMethod.Delete, $"/api/v1/organizations/members/{ownerMembershipId}", tokenA);

        revokeResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Listing_my_memberships_shows_active_and_invited_entries_across_organizations()
    {
        var (ownerA, tokenA) = await RegisterAndLoginAsync("Org A");
        var (ownerB, tokenB) = await RegisterAndLoginAsync("Org B");

        await SendAsync(HttpMethod.Post, "/api/v1/organizations/members/invite", tokenA,
            new InviteMemberRequest(ownerB.Email, OrganizationRole.Member));

        var response = await SendAsync(HttpMethod.Get, "/api/v1/me/memberships", tokenB);
        var memberships = await response.Content.ReadFromJsonAsync<List<MembershipResponse>>(TestJson.Options);

        memberships.Should().Contain(m => m.OrganizationId == ownerB.OrganizationId && m.Status == MembershipStatus.Active);
        memberships.Should().Contain(m => m.OrganizationId == ownerA.OrganizationId && m.Status == MembershipStatus.Invited);
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

        var user = new RegisteredUser(registration.UserId, registration.OrganizationId, email);
        return (user, login.AccessToken);
    }

    private sealed record RegisteredUser(Guid UserId, Guid OrganizationId, string Email);

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
