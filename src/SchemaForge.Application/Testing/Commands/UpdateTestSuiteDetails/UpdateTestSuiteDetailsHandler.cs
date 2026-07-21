using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Testing.Commands.UpdateTestSuiteDetails;

public sealed class UpdateTestSuiteDetailsHandler(ITestSuiteRepository testSuiteRepository)
    : IRequestHandler<UpdateTestSuiteDetailsCommand, Result>
{
    public async Task<Result> Handle(UpdateTestSuiteDetailsCommand request, CancellationToken cancellationToken)
    {
        var suite = await testSuiteRepository.GetByIdAsync(request.TestSuiteId, cancellationToken);
        if (suite is null)
        {
            return Result.Failure(Error.NotFound("TestSuite.NotFound", "No such test suite."));
        }

        var renameResult = suite.Rename(request.Name);
        if (renameResult.IsFailure)
        {
            return renameResult;
        }

        suite.UpdateDescription(request.Description);
        testSuiteRepository.ApplyExpectedVersion(suite, request.ExpectedVersion);

        return Result.Success();
    }
}
