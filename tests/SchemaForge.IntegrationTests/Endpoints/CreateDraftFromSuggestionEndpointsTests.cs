using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SchemaForge.Contracts.V1.Auth;
using SchemaForge.Contracts.V1.Projects;
using SchemaForge.Contracts.V1.Schemas;
using SchemaForge.IntegrationTests.Fixtures;

namespace SchemaForge.IntegrationTests.Endpoints;

// End-to-end HTTP coverage for Step 9 §2's CreateDraftFromSuggestion endpoint. NullSchemaSuggestionProvider
// always fails, so there's no way to get a real SchemaSuggestion back from suggest-schema in a test - these
// hand-construct the SchemaSuggestionResponse tree a client would have received from a real provider instead,
// exercising exactly what the endpoint itself does with it.
[Collection(nameof(IntegrationTestCollection))]
public sealed class CreateDraftFromSuggestionEndpointsTests : IAsyncLifetime
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
    public async Task Accepting_a_subset_of_suggested_nodes_materializes_only_those_onto_a_new_draft()
    {
        var token = await RegisterAndLoginAsync("Org A");
        var schemaDefinitionId = await CreateSchemaDefinitionAsync(token, "Invoice Schema");

        var acceptedId = Guid.NewGuid();
        var rejectedId = Guid.NewGuid();
        var prunedChildId = Guid.NewGuid();

        var suggestion = new SchemaSuggestionResponse(
            "hand-crafted-test-provider",
            0.9m,
            [
                new SuggestedNodeResponse(acceptedId, "invoiceNumber", NodeKind.String, "Unique invoice number", 0.95m, []),
                new SuggestedNodeResponse(
                    rejectedId, "internalNotes", NodeKind.String, "Should not appear", 0.4m,
                    [new SuggestedNodeResponse(prunedChildId, "shouldAlsoBePruned", NodeKind.String, null, 0.4m, [])]),
            ]);

        var request = new CreateDraftFromSuggestionRequest(suggestion, [acceptedId], VersionBumpKind.Minor, "from AI suggestion");
        var response = await SendAsync(HttpMethod.Post, $"/api/v1/schemas/{schemaDefinitionId}/versions/from-suggestion", token, request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CreateDraftFromSuggestionResponse>(TestJson.Options);
        result!.AcceptedCount.Should().Be(1);
        result.VersionNumber.Should().Be("1.0.0");

        var detail = await GetVersionAsync(result.SchemaVersionId, token);
        detail.RootNode.Properties.Should().ContainSingle(p => p.PropertyName == "invoiceNumber" && p.Description == "Unique invoice number");
        detail.RootNode.Properties.Should().NotContain(p => p.PropertyName == "internalNotes");
    }

    // Regression test for a bug where AddAcceptedSubtree unconditionally attached every accepted
    // node via AddObjectProperty - correct for an Object parent, but AddObjectProperty rejects a
    // non-Object parent outright (SchemaNode.NotAnObject), so any suggestion proposing an Array
    // with a nested item schema (exactly what a real provider would return for e.g. "items":
    // [...]) failed the whole request, even with every node accepted.
    [Fact]
    public async Task Accepting_a_suggested_array_with_a_nested_item_schema_materializes_the_items_node()
    {
        var token = await RegisterAndLoginAsync("Org A");
        var schemaDefinitionId = await CreateSchemaDefinitionAsync(token, "Order Schema");

        var itemsId = Guid.NewGuid();
        var itemsObjectId = Guid.NewGuid();
        var skuId = Guid.NewGuid();

        var suggestion = new SchemaSuggestionResponse(
            "hand-crafted-test-provider",
            0.8m,
            [
                new SuggestedNodeResponse(itemsId, "items", NodeKind.Array, "Line items in the order", 0.8m,
                    [
                        new SuggestedNodeResponse(itemsObjectId, null, NodeKind.Object, "A single line item", 0.75m,
                            [new SuggestedNodeResponse(skuId, "sku", NodeKind.String, "Item SKU", 0.85m, [])]),
                    ]),
            ]);

        var request = new CreateDraftFromSuggestionRequest(
            suggestion, [itemsId, itemsObjectId, skuId], VersionBumpKind.Minor, "from AI suggestion");
        var response = await SendAsync(HttpMethod.Post, $"/api/v1/schemas/{schemaDefinitionId}/versions/from-suggestion", token, request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CreateDraftFromSuggestionResponse>(TestJson.Options);
        result!.AcceptedCount.Should().Be(3);

        var detail = await GetVersionAsync(result.SchemaVersionId, token);
        var items = detail.RootNode.Properties.Should().ContainSingle(p => p.PropertyName == "items").Subject;
        items.Kind.Should().Be(NodeKind.Array);
        items.ItemsNode.Should().NotBeNull();
        items.ItemsNode!.Kind.Should().Be(NodeKind.Object);
        items.ItemsNode.Properties.Should().ContainSingle(p => p.PropertyName == "sku" && p.Description == "Item SKU");
    }

    [Fact]
    public async Task A_draft_from_suggestion_cannot_be_created_while_one_already_exists()
    {
        var token = await RegisterAndLoginAsync("Org A");
        var schemaDefinitionId = await CreateSchemaDefinitionAsync(token, "Invoice Schema");
        await SendAsync(HttpMethod.Post, $"/api/v1/schemas/{schemaDefinitionId}/versions", token,
            new CreateSchemaVersionRequest(VersionBumpKind.Minor, null));

        var suggestion = new SchemaSuggestionResponse("hand-crafted-test-provider", null, []);
        var request = new CreateDraftFromSuggestionRequest(suggestion, [], VersionBumpKind.Minor, null);
        var response = await SendAsync(HttpMethod.Post, $"/api/v1/schemas/{schemaDefinitionId}/versions/from-suggestion", token, request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private async Task<SchemaVersionDetailResponse> GetVersionAsync(Guid versionId, string token)
    {
        var response = await SendAsync(HttpMethod.Get, $"/api/v1/schema-versions/{versionId}", token);
        return (await response.Content.ReadFromJsonAsync<SchemaVersionDetailResponse>(TestJson.Options))!;
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

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string token, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: TestJson.Options);
        }

        return await _client.SendAsync(request);
    }
}
