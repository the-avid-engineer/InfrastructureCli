using System.Threading.Tasks;
using InfrastructureCli.Models;

namespace InfrastructureCli.Services;

public interface ICloudProvisioningService
{
    internal Task<string?> GetProperty(GetOptions getOptions);
    
    internal Task<bool> IsDeployed();

    internal Task<bool> IsResourceDeployed(string resourceId);

    internal Task<bool> Deploy(DeployOptions deployOptions);
}