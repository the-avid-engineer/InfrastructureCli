using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.IO;
using System.CommandLine.NamingConventionBinder;
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
        private record Arguments
        (
            IConsole Console,
            FileInfo ConfigurationsFileName,
            string ConfigurationKey,
            bool UsePreviousTemplate,
            FileInfo TemplateFileName,
            bool UsePreviousParameters,
            Dictionary<string, string> Parameters,
            FileInfo? FinalTemplateFileName
        );
        
        private static async Task<int> Execute(Arguments arguments)
        {
            var configurationsFile = await FileService.DeserializeFromFile<ConfigurationsFile>(arguments.ConfigurationsFileName);

            var configuration = configurationsFile.Configurations.GetValueOrDefault(arguments.ConfigurationKey);

            if (configuration == default)
            {
                return 1;
            }

            var template = await FileService.DeserializeFromFile<JsonElement>(arguments.TemplateFileName);

            template = template
                .RewriteGetAttributeValues(configuration.Attributes)
                .RewriteMaps()
                .RewriteSpreads()
                .RewriteGetPropertyValues()
                .RewriteSerializes();

            if (arguments.FinalTemplateFileName != null)
            {
                await FileService.SerializeToFile(template, arguments.FinalTemplateFileName);
            }
            
            arguments.Console.Out.WriteLine($"UsePreviousParameters: {arguments.UsePreviousParameters}");
            arguments.Console.Out.WriteLine($"UsePreviousTemplate: {arguments.UsePreviousTemplate}");

            var deployOptions = new DeployOptions
            (
                Configuration: configuration,
                UsePreviousTemplate: arguments.UsePreviousTemplate,
                Template: template,
                TemplateOptions: configurationsFile.TemplateOptions,
                UsePreviousParameters: arguments.UsePreviousParameters,
                Parameters: arguments.Parameters
            );
            
            var success = configurationsFile.TemplateType switch
            {
                TemplateType.AwsCloudFormation => await AwsCloudFormationService.Deploy(arguments.Console, deployOptions),
                _ => throw new NotImplementedException()
            };

            return success ? 0 : 2;
        }

        private static void AttachTemplateFileNameOption(Command parentCommand)
        {
            var templateFileName = new Option<FileInfo>("--template-file-name", () => OptionService.DefaultTemplateFileName())
            {
                Description = "The name of the file which contains the template."
            };

            parentCommand.AddOption(templateFileName);
        }

        private static void AttachUsePreviousTemplateOption(Command parentCommand)
        {
            var usePreviousTemplate = new Option<bool>("--use-previous-template")
            {
                Description =
                    "If you have already deployed the template and don't need to deploy it again, use this option."
            };
            
            usePreviousTemplate.SetDefaultValue(false);
            
            parentCommand.AddOption(usePreviousTemplate);
        }

        private static void AttachParametersOption(Command parentCommand)
        {
            var parameters = new Option<Dictionary<string, string>>("--parameters", OptionService.ParseDictionary)
            {
                Description = "Use this to specify parameter values."
            };
            
            parameters.AddAlias("-p");
            
            parentCommand.AddOption(parameters);
        }

        private static void AttachUsePreviousParametersOption(Command parentCommand)
        {
            var usePreviousParameters = new Option<bool>("--use-previous-parameters")
            {
                Description = "Use this if you want to use the previous parameter values by default."
            };
            
            usePreviousParameters.SetDefaultValue(false);
            
            parentCommand.AddOption(usePreviousParameters);
        }

        private static void AttachFinalTemplateFileNameOption(Command parentCommand)
        {
            var finalTemplateFileName = new Option<FileInfo>("--final-template-file-name")
            {
                Description = "Use this if you want the final (i.e., rewritten) template to be written to a file."
            };
            
            parentCommand.AddOption(finalTemplateFileName);
        }

        public static void Attach(RootCommand rootCommand)
        {
            var deployCommand = new Command("deploy")
            {
                Handler = CommandHandler.Create<Arguments>(Execute),
                Description = "Deploy the configuration to its appropriate service."
            };

            AttachConfigurationKeyArgument(deployCommand);
            AttachUsePreviousTemplateOption(deployCommand);
            AttachTemplateFileNameOption(deployCommand);
            AttachParametersOption(deployCommand);
            AttachUsePreviousParametersOption(deployCommand);

            AttachConfigurationsFileNameOption(deployCommand);
            AttachFinalTemplateFileNameOption(deployCommand);

            rootCommand.AddCommand(deployCommand);
        }
    }
}
