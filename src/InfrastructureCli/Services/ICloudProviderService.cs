using System.Collections.Generic;
using System.Text.Json;

namespace InfrastructureCli.Services;

public interface ICloudProviderService
{
    string GetRegionName();

    internal ICloudProvisioningService GetProvisioningService(IReadOnlyDictionary<string, JsonElement> templateOptions);
}