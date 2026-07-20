namespace SchemaForge.Contracts.V1.Testing;

public sealed record TestSuiteSummaryResponse(Guid Id, string Name, string? Description, int CaseCount);
