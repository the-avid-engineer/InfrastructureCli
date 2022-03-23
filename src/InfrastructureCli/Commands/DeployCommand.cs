using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.IO;
using System.CommandLine.NamingConventionBinder;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using InfrastructureCli.Models;
using InfrastructureCli.Rewriters;
using InfrastructureCli.Services;
using InfrastructureCli.Extensions;

namespace InfrastructureCli.Commands;

internal class DeployCommand : CommandBase
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
        
    private static async Task<int> Execute(Arguments arguments)
    {
        var configurationsFile = await FileService.DeserializeFromFile<ConfigurationsFile>(arguments.ConfigurationsFileName);

        var configuration = configurationsFile.Configurations.GetValueOrDefault(arguments.ConfigurationKey);

        if (configuration == default)
        {
            return 1;
        }

        var rewriters = new List<IRewriter>
        {
            ChainRewriter.ForCurrentPath(arguments.ConfigurationsFileName.DirectoryName!),
            ChainRewriter.Base,
            new GetAttributeValueRewriter<JsonElement>(configuration.Attributes),
            new GetAttributeValueRewriter<JsonElement>(configurationsFile.GlobalAttributes)
        };

        var region = configuration.TemplateType switch
        {
            TemplateType.AwsCloudFormation => AwsCloudFormationService.GetRegion(),
            _ => throw new NotSupportedException()
        };

        if (configuration.RegionAttributes.TryGetValue(region, out var regionAttributes))
        {
            rewriters.Add(new GetAttributeValueRewriter<JsonElement>(regionAttributes));
        }
        
        if (configurationsFile.GlobalRegionAttributes.TryGetValue(region, out var globalRegionAttributes))
        {
            rewriters.Add(new GetAttributeValueRewriter<JsonElement>(globalRegionAttributes));
        }

        var rewriter = new ChainRewriter(Enumerable.Reverse(rewriters).ToArray());
            
        var template = rewriter.Rewrite(configuration.Template);

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

    public static void Attach(RootCommand rootCommand)
    {
        var deployCommand = new Command("deploy")
        {
            Handler = CommandHandler.Create<Arguments>(Execute),
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