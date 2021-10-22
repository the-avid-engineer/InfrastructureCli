using System.CommandLine;
using System.IO;
using InfrastructureCli.Services;

namespace InfrastructureCli.Commands
{
    internal abstract class CommandBase
    {
        public static void AttachConfigurationNameArgument(Command parentCommand)
        {
            var configurationKey = new Argument<string>("configuration-key")
            {
                Description = "The key for the configuration in the configurations file."
            };

            parentCommand.AddArgument(configurationKey);
        }

        public static void AttachConfigurationsFileNameOption(Command parentCommand)
        {
            var configurationsFileName = new Option<FileInfo>("--configurations-file-name", () => OptionService.DefaultConfigurationFileName())
            {
                Description = "The name of the file which contains the configuration type and dictionary of configurations."
            };

            configurationsFileName.AddAlias("-c");

            parentCommand.AddGlobalOption(configurationsFileName);
        }
    }
}
