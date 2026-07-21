using SchemaForge.Application.Common.Messaging;
using SchemaForge.Domain.Testing;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Testing.Queries.GetTestSuite;

public sealed record GetTestSuiteQuery(Guid TestSuiteId) : IQuery<Result<TestSuiteDetail>>;

public sealed record TestSuiteDetail(
    Guid Id, Guid SchemaDefinitionId, string Name, string? Description, IReadOnlyList<TestCaseDetail> Cases,
    uint RowVersion);

public sealed record TestCaseDetail(Guid Id, string Name, string InputJson, TestExpectation Expectation);
