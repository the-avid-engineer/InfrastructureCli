using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using InfrastructureCli.Models;
using InfrastructureCli.Rewriters;
using InfrastructureCli.Services;

namespace InfrastructureCli.Commands;

public abstract record CommandBase
{
    protected static void AttachConfigurationKeyArgument(Command parentCommand)
    {
        var configurationKey = new Argument<string>("configuration-key")
        {
            Description = "The key for the configuration in the configurations file."
        };

        parentCommand.AddArgument(configurationKey);
    }

    protected static void AttachConfigurationsFileNameOption(Command parentCommand)
    {
        var configurationsFileName = new Option<FileInfo>("--configurations-file-name", () => OptionService.DefaultConfigurationsFileName())
        {
            Description = "The name of the file which contains the configuration type and dictionary of configurations."
        };

        configurationsFileName.AddAlias("-c");

        parentCommand.AddGlobalOption(configurationsFileName);
    }

    protected static ICloudProviderService GetCloudProviderService
    (
        Configuration configuration,
        IConsole console
    )
    {
        return configuration.TemplateType switch
        {
            TemplateType.AwsCloudFormation => new AwsService(console),
            _ => throw new NotSupportedException()
        };
    }

    protected static async Task<(IRootRewriter, ICloudProvisioningService)> GetProvisioningTools
    (
        ConfigurationsFile configurationsFile,
        Configuration configuration,
        IConsole console,
        string currentPath
    )
    {
        var cloudProviderService = GetCloudProviderService(configuration, console);

        var region = cloudProviderService.GetRegionName();

        var rootRewriter = RootRewriter.Create
        (
            configurationsFile.GlobalAttributes,
            configurationsFile.GlobalRegionAttributes,
            configuration.Attributes,
            configuration.RegionAttributes,
            currentPath,
            region
        );

        var templateOptions = JsonService.Convert<JsonElement, Dictionary<string, JsonElement>>
        (
            rootRewriter.Rewrite(configuration.TemplateOptions)
        );

        var cloudProvisioningService = cloudProviderService.GetProvisioningService(templateOptions);

        var deployed = await cloudProvisioningService.IsDeployed();

        rootRewriter = rootRewriter
            .PrependToBottomUp(new GetResourceDeployTypeRewriter(cloudProvisioningService))
            .PrependToBottomUp(new GetAttributeValueRewriter<object>(new Dictionary<string, object>
            {
                ["::DeployType"] = deployed ? "::Update" : "::Create"
            }));
        
        return (rootRewriter, cloudProvisioningService);
    }
}