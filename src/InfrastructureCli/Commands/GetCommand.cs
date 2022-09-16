using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.IO;
using System.Threading.Tasks;
using InfrastructureCli.Models;
using InfrastructureCli.Services;

namespace InfrastructureCli.Commands;

internal record GetCommand : CommandBase
{
    private record Arguments
    (
        FileInfo ConfigurationsFileName,
        string ConfigurationKey,
        string PropertyName,
        IConsole Console
    );
        
    private static async Task<int> Execute(Arguments arguments)
    {
        var configurationsFile = await FileService.DeserializeFromFile<ConfigurationsFile>(arguments.ConfigurationsFileName);

        var configuration = configurationsFile.Configurations.GetValueOrDefault(arguments.ConfigurationKey);

        if (configuration == default)
        {
            return 1;
        }

        var (_, cloudProvisioningService) = await GetProvisioningTools
        (
            configurationsFile,
            configuration,
            arguments.Console,
            arguments.ConfigurationsFileName.DirectoryName!
        );
        
        var getOptions = new GetOptions
        (
            arguments.PropertyName
        );

        var propertyValue = await cloudProvisioningService.GetProperty(getOptions);
            
        if (propertyValue == default)
        {
            return 2;
        }

        arguments.Console.Out.Write(propertyValue);

        return 0;
    }

    private static void AttachPropertyNameArgument(Command parentCommand)
    {
        var propertyName = new Argument<string>("property-name")
        {
            Description = "The name of the property that contains the desired information."
        };

        parentCommand.AddArgument(propertyName);
    }

    public static void Attach(RootCommand rootCommand)
    {
        var getCommand = new Command("get")
        {
            Handler = CommandHandler.Create<Arguments>(Execute),
            Description = "Retrieves information from a deployment."
        };

        AttachConfigurationsFileNameOption(getCommand);
        AttachConfigurationKeyArgument(getCommand);
        AttachPropertyNameArgument(getCommand);

        rootCommand.AddCommand(getCommand);
    }
}