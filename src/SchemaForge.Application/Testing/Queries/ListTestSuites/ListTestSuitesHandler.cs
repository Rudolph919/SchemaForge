using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Testing.Queries.ListTestSuites;

public sealed class ListTestSuitesHandler(ITestSuiteRepository testSuiteRepository)
    : IRequestHandler<ListTestSuitesQuery, Result<IReadOnlyList<TestSuiteSummary>>>
{
    public async Task<Result<IReadOnlyList<TestSuiteSummary>>> Handle(
        ListTestSuitesQuery request, CancellationToken cancellationToken)
    {
        var suites = await testSuiteRepository.GetAllForSchemaDefinitionAsync(request.SchemaDefinitionId, cancellationToken);
        return Result<IReadOnlyList<TestSuiteSummary>>.Success(suites);
    }
}
