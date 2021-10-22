using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using InfrastructureCli.Models;
using InfrastructureCli.Services;

namespace InfrastructureCli.Commands
{
    internal class GetCommand : CommandBase
    {
        private static int Execute(Dictionary<string, string> properties, string propertyName, IConsole console)
        {
            var propertyValue = properties.GetValueOrDefault(propertyName);

            if (propertyValue == default)
            {
                return 2;
            }

            console.Out.Write(propertyValue);

            return 0;
        }

        private static async Task<int> ExecuteParameter(FileInfo configurationsFileName, string configurationKey, string propertyName, IConsole console)
        {
            var configurationsFile = await FileService.DeserializeFromFile<ConfigurationsFile>(configurationsFileName);

            var configuration = configurationsFile.Configurations.GetValueOrDefault(configurationKey);

            if (configuration == default)
            {
                return 1;
            }

            return Execute(configuration.Parameters, propertyName, console);
        }

        private static async Task<int> ExecuteTag(FileInfo configurationsFileName, string configurationKey, string propertyName, IConsole console)
        {
            var configurationsFile = await FileService.DeserializeFromFile<ConfigurationsFile>(configurationsFileName);

            var configuration = configurationsFile.Configurations.GetValueOrDefault(configurationKey);

            if (configuration == default)
            {
                return 1;
            }

            return Execute(configuration.Tags, propertyName, console);
        }

        private static async Task<int> ExecuteMeta(FileInfo configurationsFileName, string configurationKey, string propertyName, IConsole console)
        {
            var configurationsFile = await FileService.DeserializeFromFile<ConfigurationsFile>(configurationsFileName);

            var configuration = configurationsFile.Configurations.GetValueOrDefault(configurationKey);

            if (configuration == default)
            {
                return 1;
            }

            return Execute(configuration.Metas, propertyName, console);
        }

        private static void AttachPropertyNameArgument(Command parentCommand)
        {
            var propertyName = new Argument<string>("property-name")
            {
                Description = "The name of the property that contains the desired information."
            };

            parentCommand.AddArgument(propertyName);
        }

        private static void AttachParameterCommand(Command parentCommand)
        {
            var parameterCommand = new Command("parameter")
            {
                Handler = CommandHandler.Create<FileInfo, string, string, IConsole>(ExecuteParameter),
                Description = "Retrieves parameter information from a configuration."
            };

            AttachPropertyNameArgument(parameterCommand);

            parentCommand.AddCommand(parameterCommand);
        }

        private static void AttachTagCommand(Command parentCommand)
        {
            var tagCommand = new Command("tag")
            {
                Handler = CommandHandler.Create<FileInfo, string, string, IConsole>(ExecuteTag),
                Description = "Retrieves tag information from a configuration."
            };

            AttachPropertyNameArgument(tagCommand);

            parentCommand.AddCommand(tagCommand);
        }

        private static void AttachMetaCommand(Command parentCommand)
        {
            var metaCommand = new Command("meta")
            {
                Handler = CommandHandler.Create<FileInfo, string, string, IConsole>(ExecuteMeta),
                Description = "Retrieves meta information from a configuration."
            };

            AttachPropertyNameArgument(metaCommand);

            parentCommand.AddCommand(metaCommand);
        }

        public static void Attach(RootCommand rootCommand)
        {
            var getCommand = new Command("get")
            {
                Description = "Retrieves information from a configuration."
            };

            AttachConfigurationNameArgument(getCommand);

            AttachParameterCommand(getCommand);
            AttachTagCommand(getCommand);
            AttachMetaCommand(getCommand);

            AttachConfigurationsFileNameOption(getCommand);

            rootCommand.AddCommand(getCommand);
        }
    }
}
