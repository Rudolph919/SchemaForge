using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Testing.Commands.RunTestSuite;

public sealed record RunTestSuiteCommand(Guid TestSuiteId, Guid TargetSchemaVersionId) : ICommand<Result<RunTestSuiteResult>>;

public sealed record RunTestSuiteResult(Guid TestRunId);
