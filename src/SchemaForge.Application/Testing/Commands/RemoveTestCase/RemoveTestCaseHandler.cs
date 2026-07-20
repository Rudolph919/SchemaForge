using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Testing.Commands.RemoveTestCase;

public sealed class RemoveTestCaseHandler(ITestSuiteRepository testSuiteRepository)
    : IRequestHandler<RemoveTestCaseCommand, Result>
{
    public async Task<Result> Handle(RemoveTestCaseCommand request, CancellationToken cancellationToken)
    {
        var suite = await testSuiteRepository.GetByIdAsync(request.TestSuiteId, cancellationToken);
        if (suite is null)
        {
            return Result.Failure(Error.NotFound("TestSuite.NotFound", "No such test suite."));
        }

        return suite.RemoveCase(request.TestCaseId);
    }
}
