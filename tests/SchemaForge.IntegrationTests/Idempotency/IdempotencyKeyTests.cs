using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using SchemaForge.Contracts.V1.Auth;
using SchemaForge.Contracts.V1.Teams;
using SchemaForge.IntegrationTests.Fixtures;

namespace SchemaForge.IntegrationTests.Idempotency;

// Step 6 §1.6: a retried POST carrying the same Idempotency-Key header must replay the original
// response instead of re-executing the side effect - proven here by checking that only ONE Team
// row results from two identical requests, not just that the two HTTP responses look the same.
[Collection(nameof(IntegrationTestCollection))]
public sealed class IdempotencyKeyTests : IAsyncLifetime
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
    public async Task A_retried_create_with_the_same_key_returns_the_original_resource_instead_of_creating_a_second_one()
    {
        var token = await RegisterAndLoginAsync();
        var idempotencyKey = Guid.NewGuid().ToString();

        var first = await CreateTeamAsync(token, idempotencyKey);
        var second = await CreateTeamAsync(token, idempotencyKey);

        first.TeamId.Should().Be(second.TeamId);

        var listResponse = await SendAsync(HttpMethod.Get, "/api/v1/teams", token);
        var list = await listResponse.Content.ReadFromJsonAsync<List<TeamSummaryResponse>>();
        list!.Count(t => t.Name == "Idempotent Team").Should().Be(1);
    }

    [Fact]
    public async Task Two_requests_without_an_idempotency_key_create_two_separate_resources()
    {
        var token = await RegisterAndLoginAsync();

        var first = await CreateTeamAsync(token, idempotencyKey: null, name: "Team One");
        var second = await CreateTeamAsync(token, idempotencyKey: null, name: "Team Two");

        first.TeamId.Should().NotBe(second.TeamId);
    }

    private async Task<CreateTeamResponse> CreateTeamAsync(string token, string? idempotencyKey, string name = "Idempotent Team")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/teams")
        {
            Content = JsonContent.Create(new CreateTeamRequest(name, null)),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (idempotencyKey is not null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        var response = await _client.SendAsync(request);
        return (await response.Content.ReadFromJsonAsync<CreateTeamResponse>())!;
    }

    private async Task<string> RegisterAndLoginAsync()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        const string password = "correct-horse-battery";

        await _client.PostAsJsonAsync(
            "/api/v1/auth/register", new RegisterRequest(email, password, "Test User", "Idempotency Org"));

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
        var login = (await loginResponse.Content.ReadFromJsonAsync<LoginResponse>())!;

        return login.AccessToken;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }
}
