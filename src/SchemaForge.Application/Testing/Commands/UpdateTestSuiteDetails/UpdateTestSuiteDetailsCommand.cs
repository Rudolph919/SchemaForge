using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Testing.Commands.UpdateTestSuiteDetails;

public sealed record UpdateTestSuiteDetailsCommand(Guid TestSuiteId, string Name, string? Description) : ICommand<Result>;
