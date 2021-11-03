using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using InfrastructureCli.Extensions;
using InfrastructureCli.Models;
using InfrastructureCli.Services;

namespace InfrastructureCli.Commands
{
    internal class DeployCommand : CommandBase
    {
        private static async Task<int> Execute(FileInfo configurationsFileName, string configurationKey, FileInfo templateFileName)
        {
            var configurationsFile = await FileService.DeserializeFromFile<ConfigurationsFile>(configurationsFileName);

            var configuration = configurationsFile.Configurations.GetValueOrDefault(configurationKey);

            if (configuration == default)
            {
                return 1;
            }

            var template = await FileService.DeserializeFromFile<JsonElement>(templateFileName);

            template = template
                .RewriteGetAttributeValues(configuration.Attributes)
                .RewriteMaps()
                .RewriteSpreads()
                .RewriteGetPropertyValues()
                .RewriteSerializes();
            
            var success = configurationsFile.Type switch
            {
                ConfigurationType.AwsCloudFormation => await AwsCloudFormationService.Deploy(configuration, template),
                _ => throw new NotImplementedException()
            };

            return success ? 0 : 2;
        }

        private static void AttachTemplateFileNameArgument(Command parentCommand)
        {
            var templateFileName = new Argument<FileInfo>("template-file-name", () => OptionService.DefaultTemplateFileName())
            {
                Description = "The name of the file which contains the template."
            };

            parentCommand.AddArgument(templateFileName);
        }

        public static void Attach(RootCommand rootCommand)
        {
            var deployCommand = new Command("deploy")
            {
                Handler = CommandHandler.Create<FileInfo, string, FileInfo>(Execute),
                Description = "Deploy the configuration to its appropriate service."
            };

            AttachConfigurationNameArgument(deployCommand);
            AttachTemplateFileNameArgument(deployCommand);

            AttachConfigurationsFileNameOption(deployCommand);

            rootCommand.AddCommand(deployCommand);
        }
    }
}
