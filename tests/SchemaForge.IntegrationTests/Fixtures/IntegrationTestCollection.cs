namespace SchemaForge.IntegrationTests.Fixtures;

[CollectionDefinition(nameof(IntegrationTestCollection))]
public sealed class IntegrationTestCollection
    : ICollectionFixture<PostgresFixture>, ICollectionFixture<MinioFixture>, ICollectionFixture<RedisFixture>;
