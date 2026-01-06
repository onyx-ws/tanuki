using System.CommandLine;

namespace Tanuki.Cli.Commands;

/// <summary>
/// Command to validate Tanuki configuration files
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

        var command = new Command("validate", "Validate Tanuki configuration files");
        command.AddOption(configFileOption);

        command.SetHandler(async (FileInfo configFile) =>
        {
            var exitCode = await ExecuteAsync(configFile);
            Environment.ExitCode = exitCode;
        }, configFileOption);

        return command;
    }

    private static async Task<int> ExecuteAsync(FileInfo? configFile)
    {
        if (configFile == null || !configFile.Exists)
        {
            Console.WriteLine($"Error: Configuration file not found: {configFile?.FullName ?? "./tanuki.json"}");
            return 1;
        }

        Console.WriteLine($"Validating configuration file: {configFile.FullName}");

        // TODO: Implement validation logic
        // For now, just check if file exists and is readable
        try
        {
            await File.ReadAllTextAsync(configFile.FullName);
            Console.WriteLine("✓ Configuration file is valid (basic check passed)");
            Console.WriteLine("Note: Full validation will be implemented in a future step");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Configuration file validation failed: {ex.Message}");
            return 1;
        }
    }
}
