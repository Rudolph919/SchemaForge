using FluentValidation;
using SchemaForge.Application.Common.Behaviors;
using SchemaForge.Application.Identity.Commands.RegisterUser;

namespace SchemaForge.Api.Extensions;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var applicationAssembly = typeof(RegisterUserCommand).Assembly;

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
