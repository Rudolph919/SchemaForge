using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Testing.Commands.RemoveTestCase;

public sealed record RemoveTestCaseCommand(Guid TestSuiteId, Guid TestCaseId) : ICommand<Result>;
