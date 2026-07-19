using Microsoft.EntityFrameworkCore;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Application.Identity;
using SchemaForge.Application.Organizations;
using SchemaForge.Application.Workspaces;
using SchemaForge.Infrastructure.Persistence;
using SchemaForge.Infrastructure.Persistence.Interceptors;
using SchemaForge.Infrastructure.Persistence.Repositories;
using SchemaForge.Infrastructure.Security;
using SchemaForge.Infrastructure.Storage;

namespace SchemaForge.Api.Extensions;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, HttpTenantContext>();
        services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();

        services.AddSingleton<AuditableEntitySaveChangesInterceptor>();
        services.AddScoped<TenantSessionConnectionInterceptor>();
        services.AddDbContext<SchemaForgeDbContext>((sp, options) =>
        {
            options
                .UseNpgsql(configuration.GetConnectionString("Default"))
                .AddInterceptors(
                    sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>(),
                    sp.GetRequiredService<TenantSessionConnectionInterceptor>());
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IOrganizationMembershipRepository, OrganizationMembershipRepository>();
        services.AddScoped<IOrganizationOwnershipGuard, OrganizationOwnershipGuard>();
        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<ISourceDocumentRepository, SourceDocumentRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IPasswordHasher, IdentityPasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));

        services.Configure<StorageSettings>(configuration.GetSection("Storage"));
        services.AddSingleton<IFileStorage, MinioFileStorage>();

        return services;
    }
}
