using Testcontainers.Minio;

namespace SchemaForge.IntegrationTests.Fixtures;

// Shared across the whole run via ICollectionFixture, same as PostgresFixture - a fresh MinIO
// container per test class would be unnecessarily slow, and SourceDocuments tests don't need
// isolation from each other beyond using unique object keys (which UploadSourceDocumentHandler
// already guarantees via a fresh guid per upload).
public sealed class MinioFixture : IAsyncLifetime
{
    private const string AccessKey = "test-access-key";
    private const string SecretKey = "test-secret-key-minimum-8-chars";

    private readonly MinioContainer _container = new MinioBuilder("minio/minio:latest")
        .WithUsername(AccessKey)
        .WithPassword(SecretKey)
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Same mechanism as PostgresFixture: environment variables, read by
        // WebApplication.CreateBuilder before any application code runs. GetConnectionString()
        // already includes the http:// scheme - prepending another one produced a malformed URI
        // whose host parsed as the literal string "http", confirmed via a live 500 during
        // verification (Amazon S3 SDK threw SocketException "nodename ... (http:80)").
        Environment.SetEnvironmentVariable("Storage__Endpoint", _container.GetConnectionString());
        Environment.SetEnvironmentVariable("Storage__AccessKey", AccessKey);
        Environment.SetEnvironmentVariable("Storage__SecretKey", SecretKey);
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}
