using System.Text.Json;
using SchemaForge.Application.Common.Messaging;
using SchemaForge.Domain.Testing;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Testing.Commands.AddTestCase;

public sealed record AddTestCaseCommand(Guid TestSuiteId, string Name, JsonElement InputPayload, TestExpectation Expectation)
    : ICommand<Result<AddTestCaseResult>>;

public sealed record AddTestCaseResult(Guid TestCaseId);
