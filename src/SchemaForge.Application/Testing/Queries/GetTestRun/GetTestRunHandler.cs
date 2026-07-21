using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Testing.Queries.GetTestRun;

public sealed class GetTestRunHandler(ITestRunRepository testRunRepository)
    : IRequestHandler<GetTestRunQuery, Result<TestRunDetail>>
{
    public async Task<Result<TestRunDetail>> Handle(GetTestRunQuery request, CancellationToken cancellationToken)
    {
        var run = await testRunRepository.GetByIdAsync(request.TestRunId, cancellationToken);
        if (run is null)
        {
            return Result<TestRunDetail>.Failure(Error.NotFound("TestRun.NotFound", "No such test run."));
        }

        return new TestRunDetail(run.Id, run.TestSuiteId, run.SchemaVersionId, run.Status, run.ExecutedAt, run.Results);
    }
}
