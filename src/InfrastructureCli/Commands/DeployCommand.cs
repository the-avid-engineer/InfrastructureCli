using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.IO;
using System.CommandLine.NamingConventionBinder;
using System.IO;
using System.Threading.Tasks;
using InfrastructureCli.Models;
using InfrastructureCli.Rewriters;
using InfrastructureCli.Services;

namespace InfrastructureCli.Commands;

internal record DeployCommand(IValidateConfigurationsFile? ValidateConfigurationsFile) : CommandBase
{
    private record Arguments
    (
        IConsole Console,
        FileInfo ConfigurationsFileName,
        string ConfigurationKey,
        bool UsePreviousParameters,
        Dictionary<string, string> Parameters,
        FileInfo? FinalTemplateFileName
    );
        
    private async Task<int> Execute(Arguments arguments)
    {
        var configurationsFile = await FileService.DeserializeFromFile<ConfigurationsFile>(arguments.ConfigurationsFileName);

        if (ValidateConfigurationsFile != null)
        {
            var valid = await ValidateConfigurationsFile.Validate(arguments.ConfigurationKey, configurationsFile);

            if (!valid)
            {
                return 3;
            }
        }
        
        var configuration = configurationsFile.Configurations.GetValueOrDefault(arguments.ConfigurationKey);

        if (configuration == default)
        {
            return 1;
        }

        var region = configuration.TemplateType switch
        {
            TemplateType.AwsCloudFormation => AwsService.GetRegionName(),
            _ => throw new NotSupportedException()
        };

        var rootRewriter = RootRewriter.Create
        (
            configurationsFile.GlobalAttributes,
            configurationsFile.GlobalRegionAttributes,
            configuration.Attributes,
            configuration.RegionAttributes,
            arguments.ConfigurationsFileName.DirectoryName!,
            region
        );
            
        var template = rootRewriter.Rewrite(configuration.Template);

        if (arguments.FinalTemplateFileName != null)
        {
            await FileService.SerializeToFile(template, arguments.FinalTemplateFileName);
        }
            
        arguments.Console.Out.WriteLine($"UsePreviousParameters: {arguments.UsePreviousParameters}");

        var deployOptions = new DeployOptions
        (
            configuration,
            template,
            arguments.UsePreviousParameters,
            arguments.Parameters
        );
            
        var success = configuration.TemplateType switch
        {
            TemplateType.AwsCloudFormation => await AwsCloudFormationService.Deploy(arguments.Console, deployOptions),
            _ => throw new NotSupportedException()
        };

        return success ? 0 : 2;
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

    public static void Attach(RootCommand rootCommand, IValidateConfigurationsFile? validateConfigurationsFile)
    {
        var deployCommand = new Command("deploy")
        {
            Handler = CommandHandler.Create<Arguments>(new DeployCommand(validateConfigurationsFile).Execute),
            Description = "Deploy the configuration to its appropriate service."
        };

        AttachConfigurationKeyArgument(deployCommand);
        AttachParametersOption(deployCommand);
        AttachUsePreviousParametersOption(deployCommand);

        AttachConfigurationsFileNameOption(deployCommand);
        AttachFinalTemplateFileNameOption(deployCommand);

        rootCommand.AddCommand(deployCommand);
    }
}