namespace SchemaForge.Contracts.V1.Schemas;

public sealed record AddLocalDefinitionRequest(string Name, NodeKind? RootKind);
