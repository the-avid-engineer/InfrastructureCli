using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.IO;
using System.CommandLine.NamingConventionBinder;
using System.IO;
using System.Threading.Tasks;
using InfrastructureCli.Models;
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
        Dictionary<string, string> Options,
        FileInfo? FinalTemplateFileName,
        int FailedExitCode
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

        var (rootRewriter, cloudProvisioningService) = await GetProvisioningTools
        (
            configurationsFile,
            configuration,
            arguments.Console,
            arguments.ConfigurationsFileName.DirectoryName!
        );

        var template = rootRewriter.Rewrite(configuration.Template);

        if (arguments.FinalTemplateFileName != null)
        {
            await FileService.SerializeToFile(template, arguments.FinalTemplateFileName);
        }

        if (!JsonService.Validate(template, out var hintPath))
        {
            arguments.Console.WriteLine($"Template is not valid JSON! See {hintPath}");
            return 4;
        }
            
        arguments.Console.Out.WriteLine($"UsePreviousParameters: {arguments.UsePreviousParameters}");

        var deployOptions = new DeployOptions
        (
            template,
            arguments.UsePreviousParameters,
            arguments.Parameters,
            arguments.Options
        );

        var success = await cloudProvisioningService.Deploy(deployOptions);

        return success ? 0 : arguments.FailedExitCode;
    }

    private static void AttachOptionsOption(Command parentCommand)
    {
        var parameters = new Option<Dictionary<string, string>>("--options", OptionService.ParseDictionary)
        {
            Description = "Use this to specify an option value."
        };
            
        parameters.AddAlias("-o");
            
        parentCommand.AddOption(parameters);
    }

    private static void AttachParametersOption(Command parentCommand)
    {
        var parameters = new Option<Dictionary<string, string>>("--parameters", OptionService.ParseDictionary)
        {
            Description = "Use this to specify a parameter value."
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

    private static void AttachFailedExitCodeOption(Command parentCommand)
    {
        var failedExitCode = new Option<int>("--failed-exit-code")
        {
            Description = "This exit code is returned if the deployment failed."
        };
        
        failedExitCode.SetDefaultValue(2);
        
        parentCommand.AddOption(failedExitCode);
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
        AttachOptionsOption(deployCommand);
        AttachUsePreviousParametersOption(deployCommand);

        AttachConfigurationsFileNameOption(deployCommand);
        AttachFinalTemplateFileNameOption(deployCommand);
        AttachFailedExitCodeOption(deployCommand);

        rootCommand.AddCommand(deployCommand);
    }
}