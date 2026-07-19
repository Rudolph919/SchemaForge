using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SchemaForge.Contracts.V1.Auth;
using SchemaForge.Contracts.V1.Projects;
using SchemaForge.IntegrationTests.Fixtures;

namespace SchemaForge.IntegrationTests.Endpoints;

[Collection(nameof(IntegrationTestCollection))]
public sealed class ProjectsEndpointsTests : IAsyncLifetime
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
    public async Task Full_lifecycle_create_update_archive_reactivate_is_reflected_in_get_and_list()
    {
        var token = await RegisterAndLoginAsync("Org A");

        var createResponse = await SendAsync(HttpMethod.Post, "/api/v1/projects", token,
            new CreateProjectRequest("Billing Schemas", "Invoices and payments"));
        var project = await createResponse.Content.ReadFromJsonAsync<CreateProjectResponse>();

        var updateResponse = await SendAsync(HttpMethod.Put, $"/api/v1/projects/{project!.ProjectId}", token,
            new UpdateProjectDetailsRequest("Billing Schemas v2", "Updated"));
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var archiveResponse = await SendAsync(HttpMethod.Post, $"/api/v1/projects/{project.ProjectId}/archive", token);
        archiveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterArchive = await GetAsync(project.ProjectId, token);
        afterArchive.Name.Should().Be("Billing Schemas v2");
        afterArchive.Status.Should().Be(ProjectStatus.Archived);

        var reactivateResponse = await SendAsync(HttpMethod.Post, $"/api/v1/projects/{project.ProjectId}/reactivate", token);
        reactivateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterReactivate = await GetAsync(project.ProjectId, token);
        afterReactivate.Status.Should().Be(ProjectStatus.Active);

        var listResponse = await SendAsync(HttpMethod.Get, "/api/v1/projects", token);
        var list = await listResponse.Content.ReadFromJsonAsync<List<ProjectSummaryResponse>>(TestJson.Options);
        list.Should().ContainSingle(p => p.Id == project.ProjectId && p.Status == ProjectStatus.Active);
    }

    [Fact]
    public async Task Creating_a_project_with_a_duplicate_name_in_the_same_org_is_rejected()
    {
        var token = await RegisterAndLoginAsync("Org A");

        await SendAsync(HttpMethod.Post, "/api/v1/projects", token, new CreateProjectRequest("Billing Schemas", null));
        var duplicateResponse = await SendAsync(
            HttpMethod.Post, "/api/v1/projects", token, new CreateProjectRequest("Billing Schemas", null));

        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task A_project_from_another_organization_is_not_visible()
    {
        var tokenA = await RegisterAndLoginAsync("Org A");
        var tokenB = await RegisterAndLoginAsync("Org B");

        var createResponse = await SendAsync(
            HttpMethod.Post, "/api/v1/projects", tokenA, new CreateProjectRequest("Org A's Project", null));
        var project = await createResponse.Content.ReadFromJsonAsync<CreateProjectResponse>();

        var getAsOrgB = await SendAsync(HttpMethod.Get, $"/api/v1/projects/{project!.ProjectId}", tokenB);

        getAsOrgB.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<ProjectDetailResponse> GetAsync(Guid projectId, string token)
    {
        var response = await SendAsync(HttpMethod.Get, $"/api/v1/projects/{projectId}", token);
        return (await response.Content.ReadFromJsonAsync<ProjectDetailResponse>(TestJson.Options))!;
    }

    private async Task<string> RegisterAndLoginAsync(string organizationName)
    {
        var email = $"{Guid.NewGuid()}@example.com";
        const string password = "correct-horse-battery";

        await _client.PostAsJsonAsync(
            "/api/v1/auth/register", new RegisterRequest(email, password, "Test User", organizationName));

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
        var login = (await loginResponse.Content.ReadFromJsonAsync<LoginResponse>())!;

        return login.AccessToken;
    }

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
