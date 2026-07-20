using System.Text.Json;

namespace SchemaForge.Contracts.V1.Testing;

public sealed record AddTestCaseRequest(string Name, JsonElement InputJson, TestExpectationDto Expectation);

public sealed record AddTestCaseResponse(Guid TestCaseId);

public sealed record UpdateTestCaseRequest(string Name, JsonElement InputJson, TestExpectationDto Expectation);
