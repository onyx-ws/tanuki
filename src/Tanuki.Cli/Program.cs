using System.CommandLine;
using Tanuki.Cli.Commands;

var rootCommand = new RootCommand("Tanuki - Developer-Flow-First API Simulator");

rootCommand.AddCommand(ServeCommand.Create());
rootCommand.AddCommand(ValidateCommand.Create());
rootCommand.AddCommand(InitCommand.Create());

return rootCommand.Invoke(args);
