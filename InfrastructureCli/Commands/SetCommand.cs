using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using InfrastructureCli.Models;
using InfrastructureCli.Services;

namespace InfrastructureCli.Commands
{
    internal class SetCommand : CommandBase
    {
        private static async Task<int> ExecuteParameter(FileInfo configurationsFileName, string configurationKey, string propertyName, string propertyValue)
        {
            var configurationsFile = await FileService.DeserializeFromFile<ConfigurationsFile>(configurationsFileName);

            var configuration = configurationsFile.Configurations.GetValueOrDefault(configurationKey);

            if (configuration == default)
            {
                return 1;
            }

            configuration.Parameters[propertyName] = propertyValue;

            await FileService.SerializeToFile(configurationsFile, configurationsFileName);

            return 0;
        }

        private static async Task<int> ExecuteTag(FileInfo configurationsFileName, string configurationKey, string propertyName, string propertyValue)
        {
            var configurationsFile = await FileService.DeserializeFromFile<ConfigurationsFile>(configurationsFileName);

            var configuration = configurationsFile.Configurations.GetValueOrDefault(configurationKey);

            if (configuration == default)
            {
                return 1;
            }

            configuration.Tags[propertyName] = propertyValue;

            await FileService.SerializeToFile(configurationsFile, configurationsFileName);

            return 0;
        }

        private static async Task<int> ExecuteMeta(FileInfo configurationsFileName, string configurationKey, string propertyName, string propertyValue)
        {
            var configurationsFile = await FileService.DeserializeFromFile<ConfigurationsFile>(configurationsFileName);

            var configuration = configurationsFile.Configurations.GetValueOrDefault(configurationKey);

            if (configuration == default)
            {
                return 1;
            }

            configuration.Metas[propertyName] = propertyValue;

            await FileService.SerializeToFile(configurationsFile, configurationsFileName);

            return 0;
        }

        private static void AttachPropertyNameArgument(Command parentCommand)
        {
            var propertyName = new Argument<string>("property-name")
            {
                Description = "The name of the property that will contain the information."
            };

            parentCommand.AddArgument(propertyName);
        }

        private static void AttachPropertyValueArgument(Command parentCommand)
        {
            var propertyValue = new Argument<string>("property-value")
            {
                Description = "The value to set for the property."
            };

            parentCommand.AddArgument(propertyValue);
        }

        private static void AttachParameterCommand(Command parentCommand)
        {
            var parameterCommand = new Command("parameter")
            {
                Handler = CommandHandler.Create<FileInfo, string, string, string>(ExecuteParameter),
                Description = "Sets parameter information for a configuration."
            };

            AttachPropertyNameArgument(parameterCommand);
            AttachPropertyValueArgument(parameterCommand);

            parentCommand.AddCommand(parameterCommand);
        }

        private static void AttachTagCommand(Command parentCommand)
        {
            var tagCommand = new Command("tag")
            {
                Handler = CommandHandler.Create<FileInfo, string, string, string>(ExecuteTag),
                Description = "Sets tag information for a configuration."
            };

            AttachPropertyNameArgument(tagCommand);
            AttachPropertyValueArgument(tagCommand);

            parentCommand.AddCommand(tagCommand);
        }

        private static void AttachMetaCommand(Command parentCommand)
        {
            var metaCommand = new Command("meta")
            {
                Handler = CommandHandler.Create<FileInfo, string, string, string>(ExecuteMeta),
                Description = "Sets meta information for a configuration."
            };

            AttachPropertyNameArgument(metaCommand);
            AttachPropertyValueArgument(metaCommand);

            parentCommand.AddCommand(metaCommand);
        }

        public static void Attach(RootCommand rootCommand)
        {
            var getCommand = new Command("set")
            {
                Description = "Sets information for a configuration."
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
