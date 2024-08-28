using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.IO;
using System.Threading.Tasks;
using InfrastructureCli.Models;
using InfrastructureCli.Services;

namespace InfrastructureCli.Commands;

internal record GetAttributeCommand : CommandBase
{
    private record Arguments
    (
        FileInfo ConfigurationsFileName,
        string ConfigurationKey,
        string AttributeName,
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

        var cloudProviderService = GetCloudProviderService(configuration, arguments.Console);
        
        var region = cloudProviderService.GetRegionName();
        
        if (configurationsFile.GlobalAttributes.TryGetValue(arguments.AttributeName, out var globalAttribute))
        {
            arguments.Console.Out.Write(globalAttribute.ToString());
            return 0;
        }
        
        if (configurationsFile.GlobalRegionAttributes.TryGetValue(region, out var globalRegionAttributes) && globalRegionAttributes.TryGetValue(arguments.AttributeName,
                out var globalRegionAttribute))
        {
            arguments.Console.Out.Write(globalRegionAttribute.ToString());
            return 0;
        }

        if (configuration.Attributes.TryGetValue(arguments.AttributeName, out var attribute))
        {
            arguments.Console.Out.Write(attribute.ToString());
            return 0;
        }

        if (configuration.RegionAttributes.TryGetValue(region, out var regionAttributes) && regionAttributes.TryGetValue(arguments.AttributeName, out var regionAttribute))
        {
            arguments.Console.Out.Write(regionAttribute.ToString());
            return 0;
        }

        return 2;
    }

    private static void AttachAttributeNameArgument(Command parentCommand)
    {
        var propertyName = new Argument<string>("attribute-name")
        {
            Description = "The name of the attribute that contains the desired information."
        };

        parentCommand.AddArgument(propertyName);
    }

    public static void Attach(RootCommand rootCommand)
    {
        var getCommand = new Command("get-attribute")
        {
            Handler = CommandHandler.Create<Arguments>(Execute),
            Description = "Retrieves an attribute value for a deployment."
        };

        AttachConfigurationsFileNameOption(getCommand);
        AttachConfigurationKeyArgument(getCommand);
        AttachAttributeNameArgument(getCommand);

        rootCommand.AddCommand(getCommand);
    }
}