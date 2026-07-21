using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchemaForge.Api.Common;
using SchemaForge.Api.Mapping;
using SchemaForge.Application.Testing.Queries.GetTestRun;

namespace SchemaForge.Api.Controllers.V1;

[ApiController]
[Authorize]
[Route("api/v1/test-runs")]
public sealed class TestRunsController(ISender sender) : ControllerBase
{
    [HttpGet("{testRunId:guid}")]
    public async Task<IActionResult> Get(Guid testRunId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTestRunQuery(testRunId), cancellationToken);
        return result.ToActionResult(r => r.ToResponse());
    }
}
