using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.IO;
using System.Threading.Tasks;
using InfrastructureCli.Models;
using InfrastructureCli.Services;

namespace InfrastructureCli.Commands
{
    internal class CanDeployCommand : CommandBase
    {
        private record Arguments
        (
            IConsole Console,
            FileInfo ConfigurationsFileName,
            string ConfigurationKey
        );
        
        private static async Task<int> Execute(Arguments arguments)
        {
            var configurationsFile = await FileService.DeserializeFromFile<ConfigurationsFile>(arguments.ConfigurationsFileName);

            var configuration = configurationsFile.Configurations.GetValueOrDefault(arguments.ConfigurationKey);

            arguments.Console.Out.Write((configuration == default).ToString());

            return 0;
        }


        public static void Attach(RootCommand rootCommand)
        {
            var deployCommand = new Command("can-deploy")
            {
                Handler = CommandHandler.Create<Arguments>(Execute),
                Description = "Outputs true or false, depending on if a configuration key exists or not.."
            };

            AttachConfigurationKeyArgument(deployCommand);
            AttachConfigurationsFileNameOption(deployCommand);

            rootCommand.AddCommand(deployCommand);
        }
    }
}
