using SchemaForge.SharedKernel;

namespace SchemaForge.Domain.Workspaces;

public sealed class Project : TenantOwnedAggregateRoot<Guid>
{
    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public ProjectStatus Status { get; private set; }

    private Project() { } // EF Core materialization

    private Project(Guid id, Guid organizationId, string name, string? description) : base(id, organizationId)
    {
        Name = name;
        Description = description;
        Status = ProjectStatus.Active;
    }

    public static Project Create(Guid organizationId, string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Project name is required.", nameof(name));
        }

        var project = new Project(Guid.NewGuid(), organizationId, name, description);
        project.RaiseDomainEvent(new ProjectCreated(organizationId, project.Id, name));

        return project;
    }

    public Result Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            return Result.Failure(Error.Validation("Project.NameRequired", "Project name is required."));
        }

        Name = newName;
        return Result.Success();
    }

    public void UpdateDescription(string? description) => Description = description;

    public Result Archive()
    {
        if (Status == ProjectStatus.Archived)
        {
            return Result.Failure(Error.Validation(
                "Project.AlreadyArchived", "This project is already archived."));
        }

        Status = ProjectStatus.Archived;
        RaiseDomainEvent(new ProjectArchived(Id));

        return Result.Success();
    }

    public Result Reactivate()
    {
        if (Status == ProjectStatus.Active)
        {
            return Result.Failure(Error.Validation(
                "Project.AlreadyActive", "This project is already active."));
        }

        Status = ProjectStatus.Active;
        RaiseDomainEvent(new ProjectReactivated(Id));

        return Result.Success();
    }
}
