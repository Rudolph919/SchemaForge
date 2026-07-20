using System.Text.Json;
using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Testing.Commands.UpdateTestCase;

public sealed class UpdateTestCaseHandler(ITestSuiteRepository testSuiteRepository)
    : IRequestHandler<UpdateTestCaseCommand, Result>
{
    public async Task<Result> Handle(UpdateTestCaseCommand request, CancellationToken cancellationToken)
    {
        var suite = await testSuiteRepository.GetByIdAsync(request.TestSuiteId, cancellationToken);
        if (suite is null)
        {
            return Result.Failure(Error.NotFound("TestSuite.NotFound", "No such test suite."));
        }

        var inputJson = JsonSerializer.Serialize(request.InputPayload);
        return suite.UpdateCase(request.TestCaseId, request.Name, inputJson, request.Expectation);
    }
}
