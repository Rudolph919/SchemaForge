namespace SchemaForge.Contracts.V1.Schemas;

public sealed record SchemaDiffResponse(
    IReadOnlyList<string> AddedPaths, IReadOnlyList<string> RemovedPaths, IReadOnlyList<SchemaDiffChangeResponse> ChangedPaths);

public sealed record SchemaDiffChangeResponse(string Path, IReadOnlyList<string> Changes);
