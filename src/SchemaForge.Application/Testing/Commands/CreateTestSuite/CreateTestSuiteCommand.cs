using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Testing.Commands.CreateTestSuite;

public sealed record CreateTestSuiteCommand(Guid SchemaDefinitionId, string Name, string? Description)
    : ICommand<Result<CreateTestSuiteResult>>;

public sealed record CreateTestSuiteResult(Guid TestSuiteId);
