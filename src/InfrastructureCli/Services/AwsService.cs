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
    
    public string GetRegionName()
    {
        return FallbackRegionFactory.GetRegionEndpoint().SystemName;
    }

    ICloudProvisioningService ICloudProviderService.GetProvisioningService(
        IReadOnlyDictionary<string, JsonElement> templateOptions)
    {
        return new AwsCloudProvisioningFormationService(_console, templateOptions);
    }
}
