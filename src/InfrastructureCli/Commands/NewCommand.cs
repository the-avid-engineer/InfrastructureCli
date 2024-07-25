using System.Collections.Generic;
using System.CommandLine;

namespace InfrastructureCli.Commands;

internal record NewCommand : CommandBase
{
    public static void Attach(RootCommand rootCommand, IEnumerable<IChildCommand> childCommands)
    {
        var newCommand = new Command("new")
        {
            Description = "Generate the files needed in order to execute other commands."
        };

        foreach (var command in childCommands)
        {
            command.Attach(newCommand);
        }

        rootCommand.AddCommand(newCommand);
    }
}