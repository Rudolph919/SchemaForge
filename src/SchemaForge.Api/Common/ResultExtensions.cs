using Microsoft.AspNetCore.Mvc;
using SchemaForge.SharedKernel;

namespace SchemaForge.Api.Common;

public static class ResultExtensions
{
    public static IActionResult ToActionResult<TValue, TResponse>(
        this Result<TValue> result, Func<TValue, TResponse> map) =>
        result.IsSuccess
            ? new OkObjectResult(map(result.Value))
            : CreateProblemResult(result.Error);

    public static IActionResult ToActionResult(this Result result) =>
        result.IsSuccess
            ? new NoContentResult()
            : CreateProblemResult(result.Error);

    private static ObjectResult CreateProblemResult(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = error.Code,
            Detail = error.Message
        };
        problemDetails.Extensions["errorCode"] = error.Code;

        return new ObjectResult(problemDetails)
        {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" }
        };
    }
}
