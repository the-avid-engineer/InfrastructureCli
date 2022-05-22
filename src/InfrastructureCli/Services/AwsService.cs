using Amazon.Runtime;

namespace InfrastructureCli.Services;

public static class AwsService
{
    public static string GetRegionName()
    {
        return FallbackRegionFactory.GetRegionEndpoint().SystemName;
    }
}
