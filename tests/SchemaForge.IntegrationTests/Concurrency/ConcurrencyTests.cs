using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SchemaForge.Contracts.V1.Auth;
using SchemaForge.Contracts.V1.Projects;
using SchemaForge.IntegrationTests.Fixtures;

namespace SchemaForge.IntegrationTests.Concurrency;

// Step 6 §1.5: proves the actual conflict path, not just "sending a fresh If-Match succeeds"
// (already covered incidentally by ProjectsEndpointsTests/SchemaVersionsEndpointsTests) - a
// stale ETag from before a concurrent update must be rejected with 409, forcing the client to
// reload rather than silently losing the other update.
[Collection(nameof(IntegrationTestCollection))]
public sealed class ConcurrencyTests : IAsyncLifetime
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
    public async Task Updating_a_project_with_a_stale_ETag_is_rejected_with_409()
    {
        var token = await RegisterAndLoginAsync();

        var createResponse = await SendAsync(HttpMethod.Post, "/api/v1/projects", token,
            new CreateProjectRequest("Concurrency Test Project", null));
        var project = (await createResponse.Content.ReadFromJsonAsync<CreateProjectResponse>())!;

        var staleETag = await GetETagAsync(project.ProjectId, token);

        // First update succeeds and bumps the row's xmin - staleETag no longer matches.
        var firstUpdate = await SendAsync(HttpMethod.Put, $"/api/v1/projects/{project.ProjectId}", token,
            new UpdateProjectDetailsRequest("Renamed Once", null), ifMatch: staleETag);
        firstUpdate.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Retrying with the now-stale ETag must fail, not silently overwrite the first update.
        var secondUpdate = await SendAsync(HttpMethod.Put, $"/api/v1/projects/{project.ProjectId}", token,
            new UpdateProjectDetailsRequest("Renamed Twice", null), ifMatch: staleETag);
        secondUpdate.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var current = await GetAsync(project.ProjectId, token);
        current.Name.Should().Be("Renamed Once");
    }

    [Fact]
    public async Task Updating_a_project_without_an_If_Match_header_is_rejected_with_428()
    {
        var token = await RegisterAndLoginAsync();

        var createResponse = await SendAsync(HttpMethod.Post, "/api/v1/projects", token,
            new CreateProjectRequest("No If-Match Project", null));
        var project = (await createResponse.Content.ReadFromJsonAsync<CreateProjectResponse>())!;

        var updateResponse = await SendAsync(HttpMethod.Put, $"/api/v1/projects/{project.ProjectId}", token,
            new UpdateProjectDetailsRequest("Attempted Rename", null));

        updateResponse.StatusCode.Should().Be((HttpStatusCode)428);
    }

    private async Task<ProjectDetailResponse> GetAsync(Guid projectId, string token)
    {
        var response = await SendAsync(HttpMethod.Get, $"/api/v1/projects/{projectId}", token);
        return (await response.Content.ReadFromJsonAsync<ProjectDetailResponse>(TestJson.Options))!;
    }

    private async Task<string?> GetETagAsync(Guid projectId, string token)
    {
        var response = await SendAsync(HttpMethod.Get, $"/api/v1/projects/{projectId}", token);
        return response.Headers.ETag?.Tag;
    }

    private async Task<string> RegisterAndLoginAsync()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        const string password = "correct-horse-battery";

        await _client.PostAsJsonAsync(
            "/api/v1/auth/register", new RegisterRequest(email, password, "Test User", "Concurrency Org"));

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
        var login = (await loginResponse.Content.ReadFromJsonAsync<LoginResponse>())!;

        return login.AccessToken;
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method, string url, string token, object? body = null, string? ifMatch = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        if (ifMatch is not null)
        {
            request.Headers.TryAddWithoutValidation("If-Match", ifMatch);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await _client.SendAsync(request);
    }
}
