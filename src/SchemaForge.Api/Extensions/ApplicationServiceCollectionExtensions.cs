using FluentValidation;
using SchemaForge.Application.Common.Behaviors;
using SchemaForge.Application.Identity.Commands.RegisterUser;
using SchemaForge.Application.Schemas.Generation;
using SchemaForge.Application.Schemas.Validation;

namespace SchemaForge.Api.Extensions;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var applicationAssembly = typeof(RegisterUserCommand).Assembly;

        // Pure computation over a SchemaNode tree, no I/O of its own - stateless, safe as a
        // singleton.
        services.AddSingleton<ISchemaValidator, SchemaValidator>();

        // Each exporter is a pure in-memory transformation over an already-loaded SchemaVersion,
        // no I/O of its own - stateless, safe as a singleton. Registered as IEnumerable<ISchemaExporter>
        // and dispatched by matching FormatKey (Step 9 §3) - a fifth format is one more line here.
        services.AddSingleton<ISchemaExporter, JsonSchemaExporter>();
        services.AddSingleton<ISchemaExporter, OpenApiExporter>();
        services.AddSingleton<ISchemaExporter, TypeScriptExporter>();
        services.AddSingleton<ISchemaExporter, CSharpExporter>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(applicationAssembly);

            // Registration order is pipeline order, outermost first: Logging wraps everything
            // (including validation failures), Validation short-circuits before Transaction ever
            // gets a chance to call SaveChanges.
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });

        services.AddValidatorsFromAssembly(applicationAssembly);

        return services;
    }
}
