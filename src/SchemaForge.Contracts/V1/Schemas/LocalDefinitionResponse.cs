namespace SchemaForge.Contracts.V1.Schemas;

public sealed record LocalDefinitionResponse(Guid Id, string Name, SchemaNodeResponse RootNode);
