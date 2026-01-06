using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Onyx.Tanuki;
using Onyx.Tanuki.Configuration;
using Onyx.Tanuki.Constants;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Configure Tanuki services with configuration
builder.Services.AddTanuki(builder.Configuration);

var app = builder.Build();

// Get logger for startup messages
var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Tanuki");
logger.LogInformation("Starting Tanuki server");

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    // Use custom exception handling middleware in non-development environments
    app.UseTanukiExceptionHandling();
}

// Add health check endpoint BEFORE simulation middleware
// This ensures health checks are not intercepted by the simulator
app.MapHealthChecks(TanukiConstants.HealthCheckPath);

// Add simulation middleware after health checks
app.UseSimulator();

logger.LogInformation("Tanuki server started and ready to accept requests");

// Start accepting requests AFTER external values are fetched (or failed)
app.Run();

// Make Program class accessible for WebApplicationFactory in tests
public partial class Program { }
