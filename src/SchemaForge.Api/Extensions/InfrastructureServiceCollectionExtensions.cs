using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using SchemaForge.Application.Common.Abstractions;
using SchemaForge.Application.Components;
using SchemaForge.Application.Identity;
using SchemaForge.Application.Organizations;
using SchemaForge.Application.Schemas;
using SchemaForge.Application.Testing;
using SchemaForge.Application.Validation;
using SchemaForge.Application.Workspaces;
using SchemaForge.Application.Audit;
using SchemaForge.Infrastructure.Ai;
using SchemaForge.Infrastructure.BackgroundJobs;
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
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, HttpTenantContext>();
        services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();

        services.AddSingleton<AuditableEntitySaveChangesInterceptor>();
        services.AddScoped<DomainEventDispatchInterceptor>();
        services.AddScoped<TenantSessionConnectionInterceptor>();
        services.AddDbContext<SchemaForgeDbContext>((sp, options) =>
        {
            options
                .UseNpgsql(configuration.GetConnectionString("Default"))
                .AddInterceptors(
                    sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>(),
                    sp.GetRequiredService<DomainEventDispatchInterceptor>(),
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
        services.AddScoped<ITestSuiteRepository, TestSuiteRepository>();
        services.AddScoped<ITestRunRepository, TestRunRepository>();
        services.AddScoped<IAuditLogEntryRepository, AuditLogEntryRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // First real consumer of the Step 1 §8 background job infrastructure (Schema Testing's
        // async test runs) - deliberately not wired any earlier, since nothing before this
        // needed asynchronous execution. Own Postgres schema ("hangfire", not "public"), migrated
        // automatically by the package itself - no hand-designed background-job table.
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(o => o.UseNpgsqlConnection(configuration.GetConnectionString("Default")),
                new PostgreSqlStorageOptions { SchemaName = "hangfire" }));
        services.AddScoped<IJobDispatcher, HangfireJobDispatcher>();

        // The actual worker (not the client above, which integration tests still need so
        // IJobDispatcher resolves) is skipped under the "Testing" WebApplicationFactory
        // environment - a real BackgroundJobServer takes ~60s to shut down gracefully per host,
        // and the integration suite spins up a fresh host per test class (confirmed live: the
        // full suite went from ~7s to a 25+ minute hang the moment this was unconditional).
        if (!environment.IsEnvironment("Testing"))
        {
            services.AddHangfireServer();
        }

        services.AddScoped<IPasswordHasher, IdentityPasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));

        services.Configure<StorageSettings>(configuration.GetSection("Storage"));
        services.AddSingleton<IFileStorage, MinioFileStorage>();

        // Step 9 §2's flagship seam - only NullSchemaSuggestionProvider exists today, registered
        // unconditionally. A real provider drops in later behind the same interface with no
        // change to Application/Domain or anything upstream of this one line.
        services.AddSingleton<ISchemaSuggestionProvider, NullSchemaSuggestionProvider>();

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
