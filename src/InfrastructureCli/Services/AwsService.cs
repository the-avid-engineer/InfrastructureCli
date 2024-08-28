using System.Collections.Generic;
using System.CommandLine;
using System.Text.Json;
using Amazon.Runtime;

namespace InfrastructureCli.Services;

public class AwsService : ICloudProviderService
{
    private readonly IConsole _console;

    public AwsService(IConsole console)
    {
        _console = console;
    }
    
    public static string GetRegionName()
    {
        return FallbackRegionFactory.GetRegionEndpoint().SystemName;
    }

    string ICloudProviderService.GetRegionName()
    {
        return GetRegionName();
    }

    ICloudProvisioningService ICloudProviderService.GetProvisioningService(
        IReadOnlyDictionary<string, JsonElement> templateOptions)
    {
        return new AwsCloudFormationService(_console, templateOptions);
    }
}
