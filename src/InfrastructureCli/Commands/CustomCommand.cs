using System.CommandLine;

namespace InfrastructureCli.Commands;

internal record CustomCommand : CommandBase
{
    public static void Attach(RootCommand rootCommand, IEnumerable<IChildCommand> childCommands)
    {
        var customCommand = new Command("custom")
        {
            Description = "Custom behavior that doesn't fall neatly into other behavior categories."
        };

        foreach (var command in childCommands)
        {
            command.Attach(customCommand);
        }

        rootCommand.AddCommand(customCommand);
    }
}