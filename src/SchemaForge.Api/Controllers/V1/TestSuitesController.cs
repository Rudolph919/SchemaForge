using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchemaForge.Api.Common;
using SchemaForge.Api.Mapping;
using SchemaForge.Application.Testing.Commands.AddTestCase;
using SchemaForge.Application.Testing.Commands.RemoveTestCase;
using SchemaForge.Application.Testing.Commands.UpdateTestCase;
using SchemaForge.Application.Testing.Queries.GetTestSuite;
using SchemaForge.Application.Testing.Queries.ListTestSuites;
using SchemaForge.Contracts.V1.Testing;

namespace SchemaForge.Api.Controllers.V1;

[ApiController]
[Authorize]
public sealed class TestSuitesController(ISender sender) : ControllerBase
{
    [HttpGet("api/v1/schemas/{schemaDefinitionId:guid}/test-suites")]
    public async Task<IActionResult> List(Guid schemaDefinitionId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListTestSuitesQuery(schemaDefinitionId), cancellationToken);
        return result.ToActionResult(suites => suites.Select(s => s.ToResponse()).ToList());
    }

    [HttpPost("api/v1/schemas/{schemaDefinitionId:guid}/test-suites")]
    public async Task<IActionResult> Create(
        Guid schemaDefinitionId, CreateTestSuiteRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCommand(schemaDefinitionId), cancellationToken);
        return result.ToActionResult(r => r.ToResponse());
    }

    [HttpGet("api/v1/test-suites/{testSuiteId:guid}")]
    public async Task<IActionResult> Get(Guid testSuiteId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTestSuiteQuery(testSuiteId), cancellationToken);
        return result.ToActionResult(d => d.ToResponse());
    }

    [HttpPatch("api/v1/test-suites/{testSuiteId:guid}")]
    public async Task<IActionResult> UpdateDetails(
        Guid testSuiteId, UpdateTestSuiteDetailsRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCommand(testSuiteId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("api/v1/test-suites/{testSuiteId:guid}/cases")]
    public async Task<IActionResult> AddCase(
        Guid testSuiteId, AddTestCaseRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCommand(testSuiteId), cancellationToken);
        return result.ToActionResult(r => r.ToResponse());
    }

    [HttpPatch("api/v1/test-suites/{testSuiteId:guid}/cases/{caseId:guid}")]
    public async Task<IActionResult> UpdateCase(
        Guid testSuiteId, Guid caseId, UpdateTestCaseRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCommand(testSuiteId, caseId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("api/v1/test-suites/{testSuiteId:guid}/cases/{caseId:guid}")]
    public async Task<IActionResult> RemoveCase(Guid testSuiteId, Guid caseId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RemoveTestCaseCommand(testSuiteId, caseId), cancellationToken);
        return result.ToActionResult();
    }
}
