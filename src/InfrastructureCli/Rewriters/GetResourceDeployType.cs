using System.Text.Json;
using InfrastructureCli.Services;

namespace InfrastructureCli.Rewriters;

internal sealed class GetResourceDeployTypeRewriter : RewriterBase, IRewriter
{
    private readonly ICloudProvisioningService _cloudProvisioningService;

    public GetResourceDeployTypeRewriter(ICloudProvisioningService cloudProvisioningService)
    {
        _cloudProvisioningService = cloudProvisioningService;
    }

    public JsonElement Rewrite(JsonElement jsonElement, IRootRewriter rootRewriter)
    {
        if (TryGetArguments(jsonElement, "GetResourceDeployType", out var argumentsElement) != true ||
            argumentsElement.ValueKind != JsonValueKind.String)
        {
            return jsonElement;
        }

        var resourceId = argumentsElement.GetString()!;

        var deployed = _cloudProvisioningService.IsResourceDeployed(resourceId).Result;

        return JsonService.Convert<string, JsonElement>(deployed ? "::Update" : "::Create");
    }
}