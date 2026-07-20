using System.Text.Json;
using SchemaForge.Application.Common.Messaging;
using SchemaForge.Domain.Testing;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Testing.Commands.UpdateTestCase;

public sealed record UpdateTestCaseCommand(
    Guid TestSuiteId, Guid TestCaseId, string Name, JsonElement InputPayload, TestExpectation Expectation) : ICommand<Result>;
