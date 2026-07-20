using System.Text.Json;
using MediatR;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Testing.Commands.AddTestCase;

public sealed class AddTestCaseHandler(ITestSuiteRepository testSuiteRepository)
    : IRequestHandler<AddTestCaseCommand, Result<AddTestCaseResult>>
{
    public async Task<Result<AddTestCaseResult>> Handle(AddTestCaseCommand request, CancellationToken cancellationToken)
    {
        var suite = await testSuiteRepository.GetByIdAsync(request.TestSuiteId, cancellationToken);
        if (suite is null)
        {
            return Result<AddTestCaseResult>.Failure(Error.NotFound("TestSuite.NotFound", "No such test suite."));
        }

        var inputJson = JsonSerializer.Serialize(request.InputPayload);
        var result = suite.AddCase(request.Name, inputJson, request.Expectation);

        return result.IsSuccess
            ? new AddTestCaseResult(result.Value)
            : Result<AddTestCaseResult>.Failure(result.Error);
    }
}
