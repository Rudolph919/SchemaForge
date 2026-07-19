using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchemaForge.Api.Common;
using SchemaForge.Api.Mapping;
using SchemaForge.Application.Workspaces.Commands.ArchiveProject;
using SchemaForge.Application.Workspaces.Commands.ReactivateProject;
using SchemaForge.Application.Workspaces.Queries.GetProject;
using SchemaForge.Application.Workspaces.Queries.ListProjects;
using SchemaForge.Contracts.V1.Projects;

namespace SchemaForge.Api.Controllers.V1;

[ApiController]
[Authorize]
[Route("api/v1/projects")]
public sealed class ProjectsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListProjectsQuery(), cancellationToken);
        return result.ToActionResult(projects => projects.Select(p => p.ToResponse()).ToList());
    }

    [HttpGet("{projectId:guid}")]
    public async Task<IActionResult> Get(Guid projectId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetProjectQuery(projectId), cancellationToken);
        return result.ToActionResult(p => p.ToResponse());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCommand(), cancellationToken);
        return result.ToActionResult(r => r.ToResponse());
    }

    [HttpPut("{projectId:guid}")]
    public async Task<IActionResult> UpdateDetails(
        Guid projectId, UpdateProjectDetailsRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCommand(projectId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{projectId:guid}/archive")]
    public async Task<IActionResult> Archive(Guid projectId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ArchiveProjectCommand(projectId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{projectId:guid}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid projectId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ReactivateProjectCommand(projectId), cancellationToken);
        return result.ToActionResult();
    }
}
