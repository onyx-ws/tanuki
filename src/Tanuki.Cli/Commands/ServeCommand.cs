using System.CommandLine;
using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Onyx.Tanuki;
using Onyx.Tanuki.Configuration;
using Onyx.Tanuki.Constants;
using Onyx.Tanuki.OpenApi;
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

        var openApiFileOption = new Option<FileInfo>(
            aliases: new[] { "--openapi", "-o" },
            description: "Path to OpenAPI specification file (JSON or YAML) or directory containing openapi.yaml/yml/json")
        {
            Arity = ArgumentArity.ZeroOrOne
        };

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
        command.AddOption(openApiFileOption);
        command.AddOption(verboseOption);

        command.SetHandler(async (int port, string host, FileInfo configFile, FileInfo? openApiFile, bool verbose) =>
        {
            await ExecuteAsync(port, host, configFile, openApiFile, verbose);
        }, portOption, hostOption, configFileOption, openApiFileOption, verboseOption);

        return command;
    }

    private static async Task ExecuteAsync(int port, string host, FileInfo? configFile, FileInfo? openApiFile, bool verbose)
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

        // Validate that both --config and --openapi are not specified
        if (configFile != null && openApiFile != null)
        {
            Console.Error.WriteLine("Error: Cannot specify both --config and --openapi. Please specify only one.");
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

        // Determine configuration source: OpenAPI takes precedence over config file
        // Use dynamic to avoid type ambiguity between Onyx.Tanuki and Tanuki.Runtime
        // Both projects define the same types in the same namespace, causing CS0433
        object? tanukiConfig = null;
        
        if (openApiFile != null)
        {
            // OpenAPI mode: parse OpenAPI spec and generate Tanuki config
            try
            {
                Console.WriteLine($"Loading OpenAPI specification from: {openApiFile.FullName}");
                
                // Create OpenAPI document loader (no logger needed for CLI - errors are shown to user)
                var fileResolver = new OpenApiFileResolver();
                var fileLoader = new OpenApiFileLoader();
                var parser = new OpenApiParser(logger: null);
                var validator = new OpenApiValidator();
                var documentLoader = new OpenApiDocumentLoader(fileResolver, fileLoader, parser, validator, logger: null);
                
                // Load OpenAPI document
                var openApiDocument = await documentLoader.LoadAsync(openApiFile.FullName);
                
                // Map OpenAPI document to Tanuki config
                // The mapper returns Onyx.Tanuki.Configuration.Tanuki
                var mapper = new OpenApiMapper();
                tanukiConfig = mapper.Map(openApiDocument);
                
                Console.WriteLine($"✓ OpenAPI specification loaded and mapped successfully");
            }
            catch (OpenApiParseException ex)
            {
                Console.Error.WriteLine($"\n✗ Failed to parse OpenAPI file: {ex.FilePath}");
                foreach (var error in ex.Errors)
                {
                    var location = !string.IsNullOrEmpty(error.Pointer) 
                        ? $" at {error.Pointer}" 
                        : error.Line.HasValue 
                            ? $" at line {error.Line}, column {error.Column}" 
                            : "";
                    Console.Error.WriteLine($"  - {error.Message}{location}");
                }
                
                if (ex.Warnings != null && ex.Warnings.Any())
                {
                    Console.Error.WriteLine("\nWarnings:");
                    foreach (var warning in ex.Warnings)
                    {
                        Console.Error.WriteLine($"  ⚠ {warning.Message}");
                    }
                }
                Environment.ExitCode = 1;
                return;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"\n✗ Error loading OpenAPI specification: {ex.Message}");
                Environment.ExitCode = 1;
                return;
            }
        }
        else if (configFile != null && configFile.Exists)
        {
            // Traditional mode: use tanuki.json file
            builder.Configuration["Tanuki:ConfigurationFilePath"] = configFile.FullName;
            builder.Configuration.AddJsonFile(configFile.FullName, optional: false, reloadOnChange: true);
        }
        else if (configFile != null && !configFile.Exists)
        {
            // File doesn't exist - show error and exit before trying to load it
            Console.WriteLine($"\nError: Configuration file not found: {configFile.FullName}");
            Console.WriteLine("\nTo create a sample configuration file, run: tanuki init");
            Console.WriteLine("Or specify a different configuration file with: tanuki serve --config <path>");
            Console.WriteLine("Or use an OpenAPI specification with: tanuki serve --openapi <path>");
            Environment.ExitCode = 1;
            return;
        }

        // Add Tanuki services
        try
        {
            if (tanukiConfig is not null)
            {
                // OpenAPI mode: use InMemoryConfigurationService
                // Add all Tanuki services first (this registers all dependencies)
                builder.Services.AddTanuki(builder.Configuration);
                
                // Override ITanukiConfigurationService with InMemoryConfigurationService
                // Use AddSingleton to replace the TryAddSingleton registration from AddTanuki
                // Note: We use reflection to avoid type ambiguity (CS0433) between Onyx.Tanuki and Tanuki.Runtime
                var configForService = tanukiConfig;
                // Get assembly from TanukiServices which is only in Onyx.Tanuki (not in Tanuki.Runtime)
                var onyxTanukiAssembly = typeof(TanukiServices).Assembly;
                var inMemoryServiceType = onyxTanukiAssembly.GetType("Onyx.Tanuki.Configuration.InMemoryConfigurationService");
                var serviceInterfaceType = onyxTanukiAssembly.GetType("Onyx.Tanuki.Configuration.ITanukiConfigurationService");
                var validatorType = onyxTanukiAssembly.GetType("Onyx.Tanuki.Configuration.IConfigurationValidator");
                var fetcherType = onyxTanukiAssembly.GetType("Onyx.Tanuki.Configuration.IExternalValueFetcher");
                
                // Validate that all required types were found
                if (inMemoryServiceType == null)
                {
                    throw new InvalidOperationException("Failed to locate InMemoryConfigurationService type in Onyx.Tanuki assembly. The type may have been renamed or moved.");
                }
                if (serviceInterfaceType == null)
                {
                    throw new InvalidOperationException("Failed to locate ITanukiConfigurationService type in Onyx.Tanuki assembly. The type may have been renamed or moved.");
                }
                if (validatorType == null)
                {
                    throw new InvalidOperationException("Failed to locate IConfigurationValidator type in Onyx.Tanuki assembly. The type may have been renamed or moved.");
                }
                if (fetcherType == null)
                {
                    throw new InvalidOperationException("Failed to locate IExternalValueFetcher type in Onyx.Tanuki assembly. The type may have been renamed or moved.");
                }
                
                var loggerType = typeof(ILogger<>).MakeGenericType(inMemoryServiceType);
                
                builder.Services.AddSingleton(serviceInterfaceType, sp =>
                {
                    var validator = sp.GetRequiredService(validatorType);
                    var externalValueFetcher = sp.GetRequiredService(fetcherType);
                    var logger = sp.GetService(loggerType);
                    
                    // Use reflection to create InMemoryConfigurationService with the config object
                    var constructors = inMemoryServiceType.GetConstructors();
                    if (constructors.Length == 0)
                    {
                        throw new InvalidOperationException($"No public constructors found on type {inMemoryServiceType.FullName}");
                    }
                    
                    var constructor = constructors[0];
                    try
                    {
                        return constructor.Invoke(new[] { configForService, validator, externalValueFetcher, logger })
                            ?? throw new InvalidOperationException($"Constructor for {inMemoryServiceType.FullName} returned null");
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Failed to create instance of {inMemoryServiceType.FullName} via reflection. Constructor parameters may have changed.", ex);
                    }
                });
            }
            else
            {
                // Traditional mode: use file-based TanukiConfigurationService
                builder.Services.AddTanuki(builder.Configuration);
            }
        }
        catch (Exception ex) when (ex is Onyx.Tanuki.Configuration.Exceptions.TanukiConfigurationException)
        {
            // Configuration error - provide helpful message
            Console.WriteLine($"\nError: {ex.Message}");
            Console.WriteLine("\nTo create a sample configuration file, run: tanuki init");
            Console.WriteLine("Or specify a different configuration file with: tanuki serve --config <path>");
            Console.WriteLine("Or use an OpenAPI specification with: tanuki serve --openapi <path>");
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
