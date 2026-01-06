using System.CommandLine;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Onyx.Tanuki;
using Onyx.Tanuki.Configuration;
using Onyx.Tanuki.Constants;
using Onyx.Tanuki.Simulation;
using Tanuki.Cli.Middleware;

namespace Tanuki.Cli.Commands;

/// <summary>
/// Command to serve the Tanuki API simulator
/// </summary>
public class ServeCommand
{
    public static Command Create()
    {
        var portOption = new Option<int>(
            aliases: new[] { "--port", "-p" },
            description: "Port to listen on")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        portOption.SetDefaultValue(5000);

        var hostOption = new Option<string>(
            aliases: new[] { "--host", "-h" },
            description: "Host to bind to")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        hostOption.SetDefaultValue("localhost");

        var configFileOption = new Option<FileInfo>(
            aliases: new[] { "--config", "-c" },
            description: "Path to tanuki.json configuration file")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        configFileOption.SetDefaultValue(new FileInfo("./tanuki.json"));

        var verboseOption = new Option<bool>(
            aliases: new[] { "--verbose", "-v" },
            description: "Enable verbose logging (shows request/response details)")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        verboseOption.SetDefaultValue(false);

        var command = new Command("serve", "Start the Tanuki API simulator server");
        command.AddOption(portOption);
        command.AddOption(hostOption);
        command.AddOption(configFileOption);
        command.AddOption(verboseOption);

        command.SetHandler(async (int port, string host, FileInfo configFile, bool verbose) =>
        {
            await ExecuteAsync(port, host, configFile, verbose);
        }, portOption, hostOption, configFileOption, verboseOption);

        return command;
    }

    private static async Task ExecuteAsync(int port, string host, FileInfo? configFile, bool verbose)
    {
        // Validate port range
        if (port < 1 || port > 65535)
        {
            Console.WriteLine($"Error: Port must be between 1 and 65535. Provided: {port}");
            Environment.ExitCode = 1;
            return;
        }

        // Validate host
        if (string.IsNullOrWhiteSpace(host))
        {
            Console.WriteLine("Error: Host cannot be empty");
            Environment.ExitCode = 1;
            return;
        }

        Console.WriteLine($"Starting Tanuki server on {host}:{port}...");

        var builder = WebApplication.CreateBuilder();

        // Configure logging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        
        // Set log levels - reduce noise, keep request/response logs visible
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.Hosting", LogLevel.Warning);
        builder.Logging.AddFilter("System.Net.Http", LogLevel.Warning);
        
        if (verbose)
        {
            builder.Logging.AddFilter("Onyx.Tanuki", LogLevel.Debug);
            builder.Logging.AddFilter("Onyx.Tanuki.Simulation", LogLevel.Information);
            Console.WriteLine("Verbose logging enabled - detailed logs will be shown");
        }
        else
        {
            builder.Logging.AddFilter("Onyx.Tanuki.Simulation", LogLevel.Information);
            builder.Logging.AddFilter("Onyx.Tanuki", LogLevel.Warning);
        }

        // Configure host filtering
        // Note: For a developer tool, allowing all hosts is acceptable, but we warn the user
        if (host == "0.0.0.0" || host == "*")
        {
            builder.Configuration["AllowedHosts"] = "*";
            Console.WriteLine("Warning: Server is configured to accept requests from any host.");
            Console.WriteLine("This is acceptable for local development but should not be used in production.");
        }
        else
        {
            // For specific hosts, restrict to that host
            builder.Configuration["AllowedHosts"] = host;
        }

        // Configure Tanuki configuration file path
        if (configFile != null && configFile.Exists)
        {
            // Set the Tanuki:ConfigurationFilePath configuration value
            builder.Configuration["Tanuki:ConfigurationFilePath"] = configFile.FullName;
            // Also add the tanuki.json file as a configuration source (for any other config it might contain)
            builder.Configuration.AddJsonFile(configFile.FullName, optional: false, reloadOnChange: true);
        }
        else if (configFile != null && !configFile.Exists)
        {
            // File doesn't exist - show error and exit before trying to load it
            Console.WriteLine($"\nError: Configuration file not found: {configFile.FullName}");
            Console.WriteLine("\nTo create a sample configuration file, run: tanuki init");
            Console.WriteLine("Or specify a different configuration file with: tanuki serve --config <path>");
            Environment.ExitCode = 1;
            return;
        }
        // If configFile is null, AddTanuki will use the default from TanukiOptions (./tanuki.json)

        // Add Tanuki services using extension method (handles type resolution)
        // This will try to load the config file (either specified or default)
        try
        {
            builder.Services.AddTanuki(builder.Configuration);
        }
        catch (Exception ex) when (ex is Onyx.Tanuki.Configuration.Exceptions.TanukiConfigurationException)
        {
            // Configuration file error - provide helpful message
            Console.WriteLine($"\nError: {ex.Message}");
            Console.WriteLine("\nTo create a sample configuration file, run: tanuki init");
            Console.WriteLine("Or specify a different configuration file with: tanuki serve --config <path>");
            Environment.ExitCode = 1;
            return;
        }

        // Configure Kestrel
        builder.WebHost.ConfigureKestrel(options =>
        {
            IPAddress ipAddress;
            try
            {
                if (host == "localhost" || host == "127.0.0.1")
                {
                    ipAddress = IPAddress.Loopback;
                }
                else if (host == "0.0.0.0" || host == "*")
                {
                    ipAddress = IPAddress.Any;
                }
                else
                {
                    if (!IPAddress.TryParse(host, out ipAddress!))
                    {
                        Console.WriteLine($"Error: Invalid host address: {host}");
                        Environment.ExitCode = 1;
                        return;
                    }
                }
                options.Listen(ipAddress, port);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Failed to configure Kestrel: {ex.Message}");
                Environment.ExitCode = 1;
                throw;
            }
        });

        var app = builder.Build();

        // Get logger
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Tanuki");
        logger.LogInformation("Starting Tanuki server on {Host}:{Port}", host, port);

        // Configure middleware
        const string healthCheckPath = "/health";
        app.MapHealthChecks(healthCheckPath);
        
        // Add request/response logging middleware (before simulator middleware)
        app.UseRequestResponseLogging(verbose);
        
        app.UseSimulator();

        logger.LogInformation("Tanuki server started and ready to accept requests");
        logger.LogInformation("Health check available at: http://{Host}:{Port}{HealthPath}", host, port, healthCheckPath);

        // Start server with cancellation token support
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
            Console.WriteLine("\nShutting down server...");
        };

        try
        {
            await app.RunAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected when user cancels
            Console.WriteLine("Server shutdown complete.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error: {ex.Message}");
            Environment.ExitCode = 1;
            throw;
        }
    }
}
