using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using SchemaForge.Contracts.V1.Auth;
using SchemaForge.Contracts.V1.Projects;
using SchemaForge.Contracts.V1.SourceDocuments;
using SchemaForge.IntegrationTests.Fixtures;

namespace SchemaForge.IntegrationTests.Endpoints;

// Exercises a real MinIO container (MinioFixture), not just the database - so a passing upload
// test here proves the file actually round-trips through storage, not just that a DB row for it
// was created.
[Collection(nameof(IntegrationTestCollection))]
public sealed class SourceDocumentsEndpointsTests : IAsyncLifetime
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
    public async Task Upload_then_list_then_delete_removes_it_from_the_listing()
    {
        var token = await RegisterAndLoginAsync("Org A");
        var projectId = await CreateProjectAsync(token, "Billing Schemas");

        var uploadResponse = await UploadAsync(
            projectId, token, "invoice-schema.json", "application/json", """{"type": "object"}""");
        var uploadBody = await uploadResponse.Content.ReadAsStringAsync();
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK, uploadBody);
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<UploadSourceDocumentResponse>();

        var listResponse = await SendAsync(HttpMethod.Get, $"/api/v1/projects/{projectId}/documents", token);
        var list = await listResponse.Content.ReadFromJsonAsync<List<SourceDocumentResponse>>();
        list.Should().ContainSingle(d => d.Id == uploaded!.DocumentId && d.FileName == "invoice-schema.json");

        var deleteResponse = await SendAsync(
            HttpMethod.Delete, $"/api/v1/documents/{uploaded!.DocumentId}", token);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listAfterDelete = await SendAsync(HttpMethod.Get, $"/api/v1/projects/{projectId}/documents", token);
        var afterDelete = await listAfterDelete.Content.ReadFromJsonAsync<List<SourceDocumentResponse>>();
        afterDelete.Should().BeEmpty();
    }

    [Fact]
    public async Task Documents_from_another_organizations_project_are_not_visible()
    {
        var tokenA = await RegisterAndLoginAsync("Org A");
        var tokenB = await RegisterAndLoginAsync("Org B");
        var projectId = await CreateProjectAsync(tokenA, "Org A's Project");

        await UploadAsync(projectId, tokenA, "schema.json", "application/json", "{}");

        var listAsOrgB = await SendAsync(HttpMethod.Get, $"/api/v1/projects/{projectId}/documents", tokenB);

        // GetByIdAsync inside ListSourceDocumentsHandler is tenant-filtered, so Org B can't even
        // discover Org A's project exists.
        listAsOrgB.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<HttpResponseMessage> UploadAsync(
        Guid projectId, string token, string fileName, string contentType, string content)
    {
        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(fileContent, "file", fileName);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/projects/{projectId}/documents")
        {
            Content = form
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await _client.SendAsync(request);
    }

    private async Task<Guid> CreateProjectAsync(string token, string name)
    {
        var response = await SendAsync(HttpMethod.Post, "/api/v1/projects", token, new CreateProjectRequest(name, null));
        var project = await response.Content.ReadFromJsonAsync<CreateProjectResponse>();
        return project!.ProjectId;
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
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await _client.SendAsync(request);
    }
}
