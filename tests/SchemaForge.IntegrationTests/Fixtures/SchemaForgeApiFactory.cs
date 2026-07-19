using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SchemaForge.IntegrationTests.Fixtures;

// Configuration (connection string, JWT settings) comes entirely from the environment variables
// PostgresFixture sets during InitializeAsync, not from ConfigureAppConfiguration here -
// WebApplication.CreateBuilder reads environment variables at the very start of Program.cs,
// before any application code runs, which sidesteps a real timing gap: ConfigureAppConfiguration
// overrides on a minimal-hosting Program.cs (top-level statements, not the two-phase
// ConfigureAppConfiguration-then-ConfigureServices pattern) aren't guaranteed to be visible by
// the time Program.cs's own code reads builder.Configuration.
public sealed class SchemaForgeApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Testing");
}
