namespace SchemaForge.Infrastructure.Storage;

public sealed class StorageSettings
{
    public required string Endpoint { get; init; }

    public required string AccessKey { get; init; }

    public required string SecretKey { get; init; }

    public string BucketName { get; init; } = "schemaforge-documents";
}
