using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using InfrastructureCli.Services;

namespace InfrastructureCli.Commands
{
    internal class NewCommand
    {
        public static void AttachOutputDirectoryNameOption(Command parentCommand)
        {
            var outputDirectoryName = new Option<DirectoryInfo>("--output-directory-name", OptionService.DefaultOutputsDirectoryName)
            {
                Description = "The name of the folder which will contain all output files."
            };

            outputDirectoryName.AddAlias("-o");

            parentCommand.AddGlobalOption(outputDirectoryName);
        }

        public static void Attach(RootCommand rootCommand, IEnumerable<IGenerateCommand> generateCommands)
        {
            var newCommand = new Command("new")
            {
                Description = "Generate the files needed in order to execute other commands."
            };

            AttachOutputDirectoryNameOption(newCommand);

            foreach (var generateCommand in generateCommands)
            {
                generateCommand.Attach(newCommand);
            }

            rootCommand.AddCommand(newCommand);
        }
    }
}
