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
        private record Arguments
        (
            FileInfo ConfigurationsFileName,
            string ConfigurationKey,
            string PropertyName,
            IConsole Console
        );
        
        private static int Execute(Dictionary<string, string> properties, Arguments arguments)
        {
            var propertyValue = properties.GetValueOrDefault(arguments.PropertyName);

            if (propertyValue == default)
            {
                return 2;
            }

            arguments.Console.Out.Write(propertyValue);

            return 0;
        }

        private static async Task<Configuration?> GetConfiguration(Arguments arguments)
        {
            var configurationsFile = await FileService.DeserializeFromFile<ConfigurationsFile>(arguments.ConfigurationsFileName);

            return configurationsFile.Configurations.GetValueOrDefault(arguments.ConfigurationKey);
        }

        private static async Task<int> ExecuteTag(Arguments arguments)
        {
            var configuration = await GetConfiguration(arguments);
            
            if (configuration == default)
            {
                return 1;
            }

            return Execute(configuration.Tags, arguments);
        }

        private static async Task<int> ExecuteMeta(Arguments arguments)
        {
            var configuration = await GetConfiguration(arguments);
            
            if (configuration == default)
            {
                return 1;
            }

            return Execute(configuration.Metas, arguments);
        }

        private static void AttachPropertyNameArgument(Command parentCommand)
        {
            var propertyName = new Argument<string>("property-name")
            {
                Description = "The name of the property that contains the desired information."
            };

            parentCommand.AddArgument(propertyName);
        }

        private static void AttachTagCommand(Command parentCommand)
        {
            var tagCommand = new Command("tag")
            {
                Handler = CommandHandler.Create<Arguments>(ExecuteTag),
                Description = "Retrieves tag information from a configuration."
            };

            AttachPropertyNameArgument(tagCommand);

            parentCommand.AddCommand(tagCommand);
        }

        private static void AttachMetaCommand(Command parentCommand)
        {
            var metaCommand = new Command("meta")
            {
                Handler = CommandHandler.Create<Arguments>(ExecuteMeta),
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

            AttachConfigurationKeyArgument(getCommand);

            AttachTagCommand(getCommand);
            AttachMetaCommand(getCommand);

            AttachConfigurationsFileNameOption(getCommand);

            rootCommand.AddCommand(getCommand);
        }
    }
}
