using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SchemaForge.Contracts.V1.Auth;
using SchemaForge.Contracts.V1.Projects;
using SchemaForge.Contracts.V1.Schemas;
using SchemaForge.Contracts.V1.Validation;
using SchemaForge.IntegrationTests.Fixtures;

namespace SchemaForge.IntegrationTests.Endpoints;

// End-to-end HTTP coverage for SchemaVersionsController (Step 6 §2.4/§2.5) - the persistence
// layer's own round-trip is already proven by SchemaVersionJsonbTests/ValidationRunRepositoryTests;
// this exercises the full request pipeline (auth, MediatR, mapping, TransactionBehavior) the way a
// real client actually calls it.
[Collection(nameof(IntegrationTestCollection))]
public sealed class SchemaVersionsEndpointsTests : IAsyncLifetime
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
    public async Task Golden_path_create_version_add_nodes_publish_and_validate_a_passing_payload()
    {
        var token = await RegisterAndLoginAsync("Org A");
        var schemaDefinitionId = await CreateSchemaDefinitionAsync(token, "Invoice Schema");

        var versionResponse = await SendAsync(HttpMethod.Post, $"/api/v1/schemas/{schemaDefinitionId}/versions", token,
            new CreateSchemaVersionRequest(VersionBumpKind.Minor, "initial draft"));
        var version = await versionResponse.Content.ReadFromJsonAsync<CreateSchemaVersionResponse>(TestJson.Options);
        version!.VersionNumber.Should().Be("1.0.0");

        var detail = await GetVersionAsync(version.SchemaVersionId, token);
        var rootId = detail.RootNode.Id;

        var addNodeResponse = await SendAsync(
            HttpMethod.Post, $"/api/v1/schema-versions/{version.SchemaVersionId}/nodes", token,
            new AddSchemaNodeRequest(rootId, NodeAttachmentKind.ObjectProperty, "invoiceNumber", NodeKind.String));
        var node = await addNodeResponse.Content.ReadFromJsonAsync<AddSchemaNodeResponse>(TestJson.Options);

        var eTag = await GetETagAsync($"/api/v1/schema-versions/{version.SchemaVersionId}", token);
        var updateResponse = await SendAsync(
            HttpMethod.Patch, $"/api/v1/schema-versions/{version.SchemaVersionId}/nodes/{node!.NodeId}", token,
            new UpdateSchemaNodeRequest(
                NodeKind.String, "Unique invoice number", null, false, true,
                Examples: [], DefaultValue: null, AllowedValues: null, ConstValue: null,
                ObjectConstraints: null, ArrayConstraints: null,
                StringConstraints: new StringConstraintsDto(3, 20, "^INV-[0-9]+$", null, null),
                NumericConstraints: null, DependentRequired: null, Composition: null,
                ComponentReference: null, LocalDefinitionRef: null),
            ifMatch: eTag);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var publishResponse = await SendAsync(
            HttpMethod.Post, $"/api/v1/schema-versions/{version.SchemaVersionId}/publish", token);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var validateResponse = await SendAsync(
            HttpMethod.Post, $"/api/v1/schema-versions/{version.SchemaVersionId}/validate", token,
            new { invoiceNumber = "INV-1001" });
        validateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var validation = await validateResponse.Content.ReadFromJsonAsync<ValidateJsonPayloadResponse>(TestJson.Options);
        validation!.Outcome.Should().Be(ValidationOutcome.Valid);
        validation.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validating_a_payload_that_violates_a_constraint_returns_200_with_Invalid_outcome_and_is_recorded()
    {
        var token = await RegisterAndLoginAsync("Org A");
        var schemaDefinitionId = await CreateSchemaDefinitionAsync(token, "Invoice Schema");
        var versionId = await CreateDraftVersionAsync(token, schemaDefinitionId);
        var rootId = (await GetVersionAsync(versionId, token)).RootNode.Id;

        var addNodeResponse = await SendAsync(
            HttpMethod.Post, $"/api/v1/schema-versions/{versionId}/nodes", token,
            new AddSchemaNodeRequest(rootId, NodeAttachmentKind.ObjectProperty, "invoiceNumber", NodeKind.String));
        var node = await addNodeResponse.Content.ReadFromJsonAsync<AddSchemaNodeResponse>(TestJson.Options);

        var eTag = await GetETagAsync($"/api/v1/schema-versions/{versionId}", token);
        await SendAsync(HttpMethod.Patch, $"/api/v1/schema-versions/{versionId}/nodes/{node!.NodeId}", token,
            new UpdateSchemaNodeRequest(
                NodeKind.String, null, null, false, true,
                Examples: [], DefaultValue: null, AllowedValues: null, ConstValue: null,
                ObjectConstraints: null, ArrayConstraints: null,
                StringConstraints: new StringConstraintsDto(null, null, "^INV-[0-9]+$", null, null),
                NumericConstraints: null, DependentRequired: null, Composition: null,
                ComponentReference: null, LocalDefinitionRef: null),
            ifMatch: eTag);

        await SendAsync(HttpMethod.Post, $"/api/v1/schema-versions/{versionId}/publish", token);

        var validateResponse = await SendAsync(
            HttpMethod.Post, $"/api/v1/schema-versions/{versionId}/validate", token,
            new { invoiceNumber = "not-a-match" });
        validateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var validation = await validateResponse.Content.ReadFromJsonAsync<ValidateJsonPayloadResponse>(TestJson.Options);
        validation!.Outcome.Should().Be(ValidationOutcome.Invalid);
        validation.Errors.Should().ContainSingle(e => e.Path == "$.invoiceNumber");

        var runsResponse = await SendAsync(HttpMethod.Get, $"/api/v1/schema-versions/{versionId}/validation-runs", token);
        var runs = await runsResponse.Content.ReadFromJsonAsync<List<ValidationRunSummaryResponse>>(TestJson.Options);
        runs.Should().ContainSingle(r => r.Id == validation.ValidationRunId && r.Outcome == ValidationOutcome.Invalid);
    }

    [Fact]
    public async Task Reordering_a_node_and_removing_another_is_reflected_in_the_reloaded_tree()
    {
        var token = await RegisterAndLoginAsync("Org A");
        var schemaDefinitionId = await CreateSchemaDefinitionAsync(token, "Invoice Schema");
        var versionId = await CreateDraftVersionAsync(token, schemaDefinitionId);
        var rootId = (await GetVersionAsync(versionId, token)).RootNode.Id;

        var first = await AddPropertyAsync(versionId, rootId, "first", NodeKind.String, token);
        var second = await AddPropertyAsync(versionId, rootId, "second", NodeKind.String, token);
        var third = await AddPropertyAsync(versionId, rootId, "third", NodeKind.String, token);

        // MoveNode sets a node's Order to exactly the requested value (Step 6 §2.4's "reorder,
        // not reparent" scope) - it doesn't renumber siblings, so this asserts the raw value
        // lands rather than assuming array-swap semantics that were never part of the contract.
        var moveResponse = await SendAsync(
            HttpMethod.Post, $"/api/v1/schema-versions/{versionId}/nodes/{third}/move", token,
            new MoveSchemaNodeRequest(9));
        moveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var eTag = await GetETagAsync($"/api/v1/schema-versions/{versionId}", token);
        var removeResponse = await SendAsync(
            HttpMethod.Delete, $"/api/v1/schema-versions/{versionId}/nodes/{second}", token, ifMatch: eTag);
        removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detail = await GetVersionAsync(versionId, token);
        detail.RootNode.Properties.Should().HaveCount(2);
        detail.RootNode.Properties.Should().NotContain(p => p.Id == second);
        detail.RootNode.Properties.Should().ContainSingle(p => p.Id == first && p.Order == 0);
        detail.RootNode.Properties.Should().ContainSingle(p => p.Id == third && p.Order == 9);
    }

    [Fact]
    public async Task A_schema_version_from_another_organization_is_not_visible()
    {
        var tokenA = await RegisterAndLoginAsync("Org A");
        var tokenB = await RegisterAndLoginAsync("Org B");

        var schemaDefinitionId = await CreateSchemaDefinitionAsync(tokenA, "Org A's Schema");
        var versionId = await CreateDraftVersionAsync(tokenA, schemaDefinitionId);

        var getAsOrgB = await SendAsync(HttpMethod.Get, $"/api/v1/schema-versions/{versionId}", tokenB);

        getAsOrgB.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task A_second_draft_cannot_be_created_while_one_already_exists()
    {
        var token = await RegisterAndLoginAsync("Org A");
        var schemaDefinitionId = await CreateSchemaDefinitionAsync(token, "Invoice Schema");
        await CreateDraftVersionAsync(token, schemaDefinitionId);

        var secondDraftResponse = await SendAsync(HttpMethod.Post, $"/api/v1/schemas/{schemaDefinitionId}/versions", token,
            new CreateSchemaVersionRequest(VersionBumpKind.Patch, null));

        secondDraftResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private async Task<Guid> AddPropertyAsync(
        Guid versionId, Guid parentNodeId, string propertyName, NodeKind kind, string token)
    {
        var response = await SendAsync(HttpMethod.Post, $"/api/v1/schema-versions/{versionId}/nodes", token,
            new AddSchemaNodeRequest(parentNodeId, NodeAttachmentKind.ObjectProperty, propertyName, kind));
        var node = await response.Content.ReadFromJsonAsync<AddSchemaNodeResponse>(TestJson.Options);
        return node!.NodeId;
    }

    private async Task<SchemaVersionDetailResponse> GetVersionAsync(Guid versionId, string token)
    {
        var response = await SendAsync(HttpMethod.Get, $"/api/v1/schema-versions/{versionId}", token);
        return (await response.Content.ReadFromJsonAsync<SchemaVersionDetailResponse>(TestJson.Options))!;
    }

    private async Task<Guid> CreateDraftVersionAsync(string token, Guid schemaDefinitionId)
    {
        var response = await SendAsync(HttpMethod.Post, $"/api/v1/schemas/{schemaDefinitionId}/versions", token,
            new CreateSchemaVersionRequest(VersionBumpKind.Minor, null));
        var version = await response.Content.ReadFromJsonAsync<CreateSchemaVersionResponse>(TestJson.Options);
        return version!.SchemaVersionId;
    }

    private async Task<Guid> CreateSchemaDefinitionAsync(string token, string name)
    {
        var projectResponse = await SendAsync(HttpMethod.Post, "/api/v1/projects", token,
            new CreateProjectRequest($"{name} Project {Guid.NewGuid():N}", null));
        var project = await projectResponse.Content.ReadFromJsonAsync<CreateProjectResponse>(TestJson.Options);

        var schemaResponse = await SendAsync(
            HttpMethod.Post, $"/api/v1/projects/{project!.ProjectId}/schemas", token,
            new CreateSchemaDefinitionRequest(name, null));
        var schema = await schemaResponse.Content.ReadFromJsonAsync<CreateSchemaDefinitionResponse>(TestJson.Options);
        return schema!.SchemaDefinitionId;
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

    // Step 6 §1.5: the resource's current ETag, to be sent back as If-Match on the next
    // PATCH/DELETE - mutating without first reading a fresh ETag now correctly gets 428.
    private async Task<string?> GetETagAsync(string url, string token)
    {
        var response = await SendAsync(HttpMethod.Get, url, token);
        return response.Headers.ETag?.Tag;
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
            request.Content = JsonContent.Create(body, options: TestJson.Options);
        }

        return await _client.SendAsync(request);
    }
}
