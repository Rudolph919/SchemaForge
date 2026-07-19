using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SchemaForge.Contracts.V1.Auth;
using SchemaForge.IntegrationTests.Fixtures;

namespace SchemaForge.IntegrationTests.Endpoints;

// No PostgresFixture constructor parameter needed here (unlike the MultiTenancy tests) - being
// in the collection is enough for xUnit to guarantee PostgresFixture.InitializeAsync (env vars,
// migrations) has already run before any test method in this class executes.
[Collection(nameof(IntegrationTestCollection))]
public sealed class AuthEndpointsTests : IAsyncLifetime
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

    private static RegisterRequest NewRegisterRequest() => new(
        $"user-{Guid.NewGuid()}@example.com", "correct-horse-battery", "Test User", "Test Org");

    [Fact]
    public async Task Register_with_valid_data_returns_ok_with_user_and_organization_ids()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", NewRegisterRequest());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        body!.UserId.Should().NotBeEmpty();
        body.OrganizationId.Should().NotBeEmpty();
        body.OrganizationSlug.Should().Be("test-org");
    }

    [Fact]
    public async Task Register_with_duplicate_email_returns_conflict()
    {
        var request = NewRegisterRequest();
        await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/register", request with { OrganizationName = "A Different Org" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_with_invalid_data_returns_bad_request()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/register", new RegisterRequest("", "short", "", ""));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_with_valid_credentials_returns_ok_with_access_token()
    {
        var registerRequest = NewRegisterRequest();
        await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginRequest(registerRequest.Email, registerRequest.Password));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_with_wrong_password_and_login_with_nonexistent_email_return_the_same_error()
    {
        var registerRequest = NewRegisterRequest();
        await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        var wrongPasswordResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginRequest(registerRequest.Email, "totally-wrong"));
        var nonexistentEmailResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginRequest($"nobody-{Guid.NewGuid()}@example.com", "whatever123"));

        wrongPasswordResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        nonexistentEmailResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var wrongPasswordBody = await wrongPasswordResponse.Content.ReadAsStringAsync();
        var nonexistentEmailBody = await nonexistentEmailResponse.Content.ReadAsStringAsync();

        // Same body for both - revealing which one it was would let an attacker enumerate
        // registered emails (Application layer's deliberate design, asserted here at the wire).
        wrongPasswordBody.Should().Be(nonexistentEmailBody);
    }
}
