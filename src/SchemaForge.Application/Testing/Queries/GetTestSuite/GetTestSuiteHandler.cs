using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Testing.Queries.GetTestSuite;

public sealed class GetTestSuiteHandler(ITestSuiteRepository testSuiteRepository)
    : IRequestHandler<GetTestSuiteQuery, Result<TestSuiteDetail>>
{
    public async Task<Result<TestSuiteDetail>> Handle(GetTestSuiteQuery request, CancellationToken cancellationToken)
    {
        var suite = await testSuiteRepository.GetByIdAsync(request.TestSuiteId, cancellationToken);
        if (suite is null)
        {
            return Result<TestSuiteDetail>.Failure(Error.NotFound("TestSuite.NotFound", "No such test suite."));
        }

        var cases = suite.Cases
            .Select(c => new TestCaseDetail(c.Id, c.Name, c.InputJson, c.Expectation))
            .ToList();

        return new TestSuiteDetail(suite.Id, suite.SchemaDefinitionId, suite.Name, suite.Description, cases, suite.RowVersion);
    }
}
