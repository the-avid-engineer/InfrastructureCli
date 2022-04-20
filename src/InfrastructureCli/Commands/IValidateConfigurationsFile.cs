using System.Threading.Tasks;
using InfrastructureCli.Models;

namespace InfrastructureCli.Commands;

public interface IValidateConfigurationsFile
{
    Task<bool> Validate(string configurationKey, ConfigurationsFile configurationsFile);
}