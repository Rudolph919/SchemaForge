using System.Text.Json;

namespace SchemaForge.Contracts.V1.Testing;

public sealed record TestCaseResponse(Guid Id, string Name, JsonElement InputJson, TestExpectationDto Expectation);

public sealed record TestSuiteDetailResponse(
    Guid Id, Guid SchemaDefinitionId, string Name, string? Description, IReadOnlyList<TestCaseResponse> Cases);
