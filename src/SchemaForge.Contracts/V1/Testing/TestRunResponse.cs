using SchemaForge.Contracts.V1.Validation;

namespace SchemaForge.Contracts.V1.Testing;

public enum TestRunStatus
{
    Pending,
    Completed
}

public sealed record TestCaseResultResponse(
    Guid TestCaseId, string TestCaseName, bool Passed, IReadOnlyList<ValidationErrorResponse> ActualErrors);

public sealed record TestRunResponse(
    Guid Id,
    Guid TestSuiteId,
    Guid SchemaVersionId,
    TestRunStatus Status,
    DateTimeOffset ExecutedAt,
    IReadOnlyList<TestCaseResultResponse> Results);
