using System.Collections.Generic;

namespace InfrastructureCli.Models
{
    public record ConfigurationsFile
    (
        ConfigurationType Type,
        Dictionary<string, Configuration> Configurations
    );
}
