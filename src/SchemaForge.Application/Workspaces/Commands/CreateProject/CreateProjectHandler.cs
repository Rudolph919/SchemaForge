using MediatR;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Domain.Workspaces;
using SchemaForge.SharedKernel;

namespace SchemaForge.Application.Workspaces.Commands.CreateProject;

public sealed class CreateProjectHandler(IProjectRepository projectRepository, ITenantContext tenantContext)
    : IRequestHandler<CreateProjectCommand, Result<CreateProjectResult>>
{
    public async Task<Result<CreateProjectResult>> Handle(
        CreateProjectCommand request, CancellationToken cancellationToken)
    {
        if (await projectRepository.ExistsByNameAsync(request.Name, cancellationToken))
        {
            return Result<CreateProjectResult>.Failure(Error.Conflict(
                "Project.NameAlreadyExists", "A project with this name already exists in this organization."));
        }

        var project = Project.Create(tenantContext.CurrentTenantId!.Value, request.Name, request.Description);
        await projectRepository.AddAsync(project, cancellationToken);

        return new CreateProjectResult(project.Id);
    }
}
