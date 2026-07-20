using SchemaForge.Application.Common.Messaging;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Testing.Queries.ListTestSuites;

public sealed record ListTestSuitesQuery(Guid SchemaDefinitionId) : IQuery<Result<IReadOnlyList<TestSuiteSummary>>>;
