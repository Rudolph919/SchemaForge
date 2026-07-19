using SchemaForge.Application.Workspaces.Commands.CreateProject;
using SchemaForge.Application.Workspaces.Commands.UpdateProjectDetails;
using SchemaForge.Application.Workspaces.Queries.GetProject;
using SchemaForge.Application.Workspaces.Queries.ListProjects;
using SchemaForge.Contracts.V1.Projects;
using DomainProjectStatus = SchemaForge.Domain.Workspaces.ProjectStatus;

namespace SchemaForge.Api.Mapping;

public static class ProjectsMappingExtensions
{
    public static CreateProjectCommand ToCommand(this CreateProjectRequest request) =>
        new(request.Name, request.Description);

    public static CreateProjectResponse ToResponse(this CreateProjectResult result) => new(result.ProjectId);

    public static UpdateProjectDetailsCommand ToCommand(this UpdateProjectDetailsRequest request, Guid projectId) =>
        new(projectId, request.Name, request.Description);

    public static ProjectSummaryResponse ToResponse(this ProjectSummary summary) =>
        new(summary.Id, summary.Name, summary.Description, summary.Status.ToContract());

    public static ProjectDetailResponse ToResponse(this ProjectDetail detail) =>
        new(detail.Id, detail.Name, detail.Description, detail.Status.ToContract());

    private static ProjectStatus ToContract(this DomainProjectStatus status) => status switch
    {
        DomainProjectStatus.Active => ProjectStatus.Active,
        DomainProjectStatus.Archived => ProjectStatus.Archived,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown project status.")
    };
}
