using System.CommandLine;
using Onyx.Tanuki.Configuration;
using Onyx.Tanuki.Configuration.Exceptions;
using Onyx.Tanuki.Configuration.Json;
using Onyx.Tanuki.OpenApi;

namespace Tanuki.Cli.Commands;

/// <summary>
/// Command to validate Tanuki configuration files and OpenAPI specifications
/// </summary>
public class ValidateCommand
{
    public static Command Create()
    {
        var configFileOption = new Option<FileInfo>(
            aliases: new[] { "--config", "-c" },
            description: "Path to tanuki.json configuration file to validate")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        configFileOption.SetDefaultValue(new FileInfo("./tanuki.json"));

        var openApiFileOption = new Option<FileInfo>(
            aliases: new[] { "--openapi", "-o" },
            description: "Path to OpenAPI specification file (JSON or YAML) or directory containing openapi.yaml/yml/json to validate")
        {
            Arity = ArgumentArity.ZeroOrOne
        };

        var jsonOutputOption = new Option<bool>(
            aliases: new[] { "--json" },
            description: "Output errors in JSON format (for machine parsing)")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        jsonOutputOption.SetDefaultValue(false);

        var command = new Command("validate", "Validate Tanuki configuration files or OpenAPI specifications");
        command.AddOption(configFileOption);
        command.AddOption(openApiFileOption);
        command.AddOption(jsonOutputOption);

        command.SetHandler(async (FileInfo? configFile, FileInfo? openApiFile, bool jsonOutput) =>
        {
            var exitCode = await ExecuteAsync(configFile, openApiFile, jsonOutput);
            Environment.ExitCode = exitCode;
        }, configFileOption, openApiFileOption, jsonOutputOption);

        return command;
    }

    private static async Task<int> ExecuteAsync(FileInfo? configFile, FileInfo? openApiFile, bool jsonOutput)
    {
        // Validate that exactly one option is provided
        if (configFile != null && openApiFile != null)
        {
            if (jsonOutput)
            {
                Console.WriteLine("{\"valid\": false, \"errors\": [{\"message\": \"Cannot specify both --config and --openapi. Please specify only one.\"}]}");
            }
            else
            {
                Console.Error.WriteLine("Error: Cannot specify both --config and --openapi. Please specify only one.");
            }
            return 1;
        }

        if (configFile == null && openApiFile == null)
        {
            // Default to config file
            configFile = new FileInfo("./tanuki.json");
        }

        if (openApiFile != null)
        {
            // Validate OpenAPI specification
            return await ValidateOpenApiAsync(openApiFile, jsonOutput);
        }
        else if (configFile != null)
        {
            // Validate Tanuki configuration file
            return await ValidateTanukiConfigAsync(configFile, jsonOutput);
        }

        return 1;
    }

    private static async Task<int> ValidateTanukiConfigAsync(FileInfo configFile, bool jsonOutput)
    {
        if (!configFile.Exists)
        {
            if (jsonOutput)
            {
                Console.WriteLine($"{{\"valid\": false, \"errors\": [{{\"message\": \"Configuration file not found: {System.Text.Json.JsonSerializer.Serialize(configFile.FullName)}\"}}]}}");
            }
            else
            {
                Console.WriteLine($"Error: Configuration file not found: {configFile.FullName}");
            }
            return 1;
        }

        if (!jsonOutput)
        {
            Console.WriteLine($"Validating configuration file: {configFile.FullName}");
        }

        try
        {
            // Load and validate the configuration
            var json = await File.ReadAllTextAsync(configFile.FullName);
            var tanuki = JsonConfigParser.Parse(json);
            
            var validator = new ConfigurationValidator();
            validator.Validate(tanuki);
            
            if (jsonOutput)
            {
                Console.WriteLine("{\"valid\": true}");
            }
            else
            {
                Console.WriteLine("✓ Configuration file is valid");
            }
            return 0;
        }
        catch (TanukiConfigurationException ex)
        {
            if (jsonOutput)
            {
                Console.WriteLine($"{{\"valid\": false, \"errors\": [{{\"message\": {System.Text.Json.JsonSerializer.Serialize(ex.Message)}}}]}}");
            }
            else
            {
                Console.WriteLine($"✗ Configuration file validation failed: {ex.Message}");
            }
            return 1;
        }
        catch (Exception ex)
        {
            if (jsonOutput)
            {
                Console.WriteLine($"{{\"valid\": false, \"errors\": [{{\"message\": {System.Text.Json.JsonSerializer.Serialize(ex.Message)}}}]}}");
            }
            else
            {
                Console.WriteLine($"✗ Configuration file validation failed: {ex.Message}");
            }
            return 1;
        }
    }

    private static async Task<int> ValidateOpenApiAsync(FileInfo openApiFile, bool jsonOutput)
    {
        if (!openApiFile.Exists && !Directory.Exists(openApiFile.FullName))
        {
            if (jsonOutput)
            {
                Console.WriteLine($"{{\"valid\": false, \"errors\": [{{\"message\": {System.Text.Json.JsonSerializer.Serialize($"OpenAPI file or directory not found: {openApiFile.FullName}")}}}]}}");
            }
            else
            {
                Console.WriteLine($"Error: OpenAPI file or directory not found: {openApiFile.FullName}");
            }
            return 1;
        }

        if (!jsonOutput)
        {
            Console.WriteLine($"Validating OpenAPI specification: {openApiFile.FullName}");
        }

        try
        {
            // Create OpenAPI document loader
            var fileResolver = new OpenApiFileResolver();
            var fileLoader = new OpenApiFileLoader();
            var parser = new OpenApiParser(logger: null);
            var validator = new OpenApiValidator();
            var documentLoader = new OpenApiDocumentLoader(fileResolver, fileLoader, parser, validator, logger: null);
            
            // Load and validate the OpenAPI document
            var document = await documentLoader.LoadAsync(openApiFile.FullName);
            
            if (jsonOutput)
            {
                Console.WriteLine("{\"valid\": true}");
            }
            else
            {
                Console.WriteLine($"✓ OpenAPI specification is valid");
            }
            return 0;
        }
        catch (OpenApiParseException ex)
        {
            if (jsonOutput)
            {
                Console.WriteLine(ex.ToJson());
            }
            else
            {
                Console.Error.WriteLine($"\n✗ Invalid OpenAPI file: {ex.FilePath}");
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
            }
            return 1;
        }
        catch (Exception ex)
        {
            if (jsonOutput)
            {
                Console.WriteLine($"{{\"valid\": false, \"errors\": [{{\"message\": {System.Text.Json.JsonSerializer.Serialize(ex.Message)}}}]}}");
            }
            else
            {
                Console.Error.WriteLine($"\n✗ Error validating OpenAPI specification: {ex.Message}");
            }
            return 1;
        }
    }
}
