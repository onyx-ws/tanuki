using System.CommandLine;

namespace Tanuki.Cli.Commands;

/// <summary>
/// Command to initialize a new Tanuki project
/// </summary>
public class InitCommand
{
    public static Command Create()
    {
        var outputOption = new Option<DirectoryInfo>(
            aliases: new[] { "--output", "-o" },
            description: "Output directory for the generated configuration")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        outputOption.SetDefaultValue(new DirectoryInfo("."));

        var command = new Command("init", "Initialize a new Tanuki project");
        command.AddOption(outputOption);

        command.SetHandler(async (DirectoryInfo outputDir) =>
        {
            var exitCode = await ExecuteAsync(outputDir);
            Environment.ExitCode = exitCode;
        }, outputOption);

        return command;
    }

    private static async Task<int> ExecuteAsync(DirectoryInfo? outputDir)
    {
        outputDir ??= new DirectoryInfo(".");

        Console.WriteLine($"Initializing Tanuki project in: {outputDir.FullName}");

        if (!outputDir.Exists)
        {
            try
            {
                outputDir.Create();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Failed to create output directory: {ex.Message}");
                return 1;
            }
        }

        var configFile = new FileInfo(Path.Combine(outputDir.FullName, "tanuki.json"));

        if (configFile.Exists)
        {
            Console.WriteLine($"Warning: Configuration file already exists: {configFile.FullName}");
            Console.WriteLine("Skipping initialization. Use --force to overwrite (not yet implemented).");
            return 0; // Not an error, just a warning
        }

        // Create a minimal example configuration matching the expected format
        // paths must be an object where keys are URIs and values contain HTTP methods
        var exampleConfig = @"{
  ""paths"": {
    ""/api/example"": {
      ""get"": {
        ""summary"": ""Example GET endpoint"",
        ""operationId"": ""getExample"",
        ""responses"": {
          ""200"": {
            ""description"": ""Successful response"",
            ""content"": {
              ""application/json"": {
                ""examples"": {
                  ""default"": {
                    ""summary"": ""Default example"",
                    ""value"": ""{\""message\"": \""Hello from Tanuki!\""}""
                  }
                }
              }
            }
          }
        }
      }
    }
  }
}";

        try
        {
            await File.WriteAllTextAsync(configFile.FullName, exampleConfig);
            Console.WriteLine($"✓ Created example configuration file: {configFile.FullName}");
            Console.WriteLine("You can now run 'tanuki serve' to start the simulator.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Failed to create configuration file: {ex.Message}");
            return 1;
        }
    }
}
