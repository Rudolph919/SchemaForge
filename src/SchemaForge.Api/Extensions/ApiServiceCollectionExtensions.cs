using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SchemaForge.Api.Middleware;
using SchemaForge.Infrastructure.Security;

namespace SchemaForge.Api.Extensions;

public static class ApiServiceCollectionExtensions
{
    private const string FrontendCorsPolicy = "Frontend";

    // Named policy for unauthenticated, abuse-prone endpoints (login/register) - much stricter
    // than the global default, since these are exactly the endpoints a credential-stuffing or
    // account-enumeration attempt would hammer. Everything else gets the more generous global
    // limiter below; both apply simultaneously where an endpoint opts into this one.
    public const string AuthRateLimitPolicy = "auth";

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
        // ETag must be explicitly exposed - it isn't in the small set of response headers
        // (Content-Type, Content-Length, etc.) browsers expose to JS on cross-origin responses by
        // default, and the frontend needs to read it to round-trip If-Match on the next mutation.
        var frontendOrigin = configuration["Cors:FrontendOrigin"] ?? "http://localhost:5173";
        services.AddCors(options => options.AddPolicy(FrontendCorsPolicy, policy =>
            policy.WithOrigins(frontendOrigin).AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("ETag")));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Partitioned by authenticated user where there is one, falling back to remote IP
            // for anonymous requests - a shared IP (office, NAT) shouldn't throttle every user
            // behind it once any one of them is authenticated and identifiable individually.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var key = httpContext.User.Identity?.IsAuthenticated == true
                    ? $"user:{httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value}"
                    : $"ip:{httpContext.Connection.RemoteIpAddress}";

                return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 200,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                });
            });

            options.AddPolicy(AuthRateLimitPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    $"ip:{httpContext.Connection.RemoteIpAddress}",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));

            options.OnRejected = (context, cancellationToken) =>
            {
                context.HttpContext.Response.Headers.RetryAfter =
                    ((int)TimeSpan.FromMinutes(1).TotalSeconds).ToString(CultureInfo.InvariantCulture);
                return ValueTask.CompletedTask;
            };
        });

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
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        // Needs the endpoint (for the [Idempotent] marker) and the authenticated user (to scope
        // the cache key) both already resolved - after UseAuthorization, before the terminal
        // controller dispatch.
        app.UseMiddleware<IdempotencyKeyMiddleware>();

        app.MapControllers();

        return app;
    }
}
