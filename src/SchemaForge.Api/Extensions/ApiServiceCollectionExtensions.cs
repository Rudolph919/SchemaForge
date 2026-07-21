using System.Text;
using System.Text.Json.Serialization;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SchemaForge.Infrastructure.Security;

namespace SchemaForge.Api.Extensions;

public static class ApiServiceCollectionExtensions
{
    private const string FrontendCorsPolicy = "Frontend";

    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.AddOpenApi();
        services.AddProblemDetails();

        var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>()
            ?? throw new InvalidOperationException("Jwt configuration section is missing.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Without this, ASP.NET Core remaps short claim names to legacy XML-namespaced
                // ClaimTypes URIs on the way in (e.g. "sub" -> ClaimTypes.NameIdentifier) - the
                // default is true for backward compatibility, not false as might be assumed.
                // HttpCurrentUserContext/HttpTenantContext read the short names JwtTokenService
                // actually writes ("sub", "org_id"), so without this they'd silently never find
                // them. Confirmed live: ICurrentUserContext.UserId was null for every
                // authenticated request until this was set.
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization();

        // Named, narrow policy for the Vite dev server - not a wildcard CORS policy.
        var frontendOrigin = configuration["Cors:FrontendOrigin"] ?? "http://localhost:5173";
        services.AddCors(options => options.AddPolicy(FrontendCorsPolicy, policy =>
            policy.WithOrigins(frontendOrigin).AllowAnyHeader().AllowAnyMethod()));

        return services;
    }

    public static WebApplication UseApi(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();

            // Dev-only diagnostic view into job history/retries, same gating as Swagger above -
            // not worth a dashboard-specific auth policy for a portfolio project's local tooling.
            app.UseHangfireDashboard();
        }

        app.UseExceptionHandler();
        app.UseHttpsRedirection();
        app.UseCors(FrontendCorsPolicy);
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }
}
