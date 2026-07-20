using Microsoft.EntityFrameworkCore;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Application.Components;
using SchemaForge.Application.Identity;
using SchemaForge.Application.Organizations;
using SchemaForge.Application.Schemas;
using SchemaForge.Application.Validation;
using SchemaForge.Application.Workspaces;
using SchemaForge.Infrastructure.Caching;
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
        services.AddScoped<ISchemaDefinitionRepository, SchemaDefinitionRepository>();
        services.AddScoped<ISchemaVersionRepository, SchemaVersionRepository>();
        services.AddScoped<IValidationRunRepository, ValidationRunRepository>();
        services.AddScoped<IComponentDefinitionRepository, ComponentDefinitionRepository>();
        services.AddScoped<IComponentVersionRepository, ComponentVersionRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IPasswordHasher, IdentityPasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));

        services.Configure<StorageSettings>(configuration.GetSection("Storage"));
        services.AddSingleton<IFileStorage, MinioFileStorage>();

        // Backs the documentation cache (Step 1 §9, Step 6 §2.4) - genuinely S3-API-compatible-style
        // swap-behind-an-interface story via IDistributedCache, same as IFileStorage's own seam.
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            // A shared local Redis instance may back more than one project's dev environment -
            // this keeps every key this app writes namespaced, not just "safe in production."
            options.InstanceName = "schemaforge:";
        });
        services.AddSingleton<IDocumentationCache, RedisDocumentationCache>();

        return services;
    }
}
