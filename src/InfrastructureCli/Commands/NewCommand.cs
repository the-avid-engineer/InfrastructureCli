using System.Collections.Generic;
using System.CommandLine;

namespace InfrastructureCli.Commands
{
    internal class NewCommand
    {
        public static void Attach(RootCommand rootCommand, IEnumerable<IGenerateCommand> generateCommands)
        {
            var newCommand = new Command("new")
            {
                Description = "Generate the files needed in order to execute other commands."
            };

            foreach (var generateCommand in generateCommands)
            {
                generateCommand.Attach(newCommand);
            }

            rootCommand.AddCommand(newCommand);
        }
    }
}
