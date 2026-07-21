using SchemaForge.Application.Common.Messaging;
using SchemaForge.Domain.Testing;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Testing.Queries.GetTestRun;

public sealed record GetTestRunQuery(Guid TestRunId) : IQuery<Result<TestRunDetail>>;

public sealed record TestRunDetail(
    Guid Id,
    Guid TestSuiteId,
    Guid SchemaVersionId,
    TestRunStatus Status,
    DateTimeOffset ExecutedAt,
    IReadOnlyList<TestCaseResult> Results);
