using Serilog;
using SchemaForge.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfiguration) =>
    loggerConfiguration.ReadFrom.Configuration(context.Configuration).Enrich.FromLogContext());

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApi(builder.Configuration);

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseApi();

app.Run();

// Exposes the entry point to SchemaForge.IntegrationTests' WebApplicationFactory<Program>.
public partial class Program;
