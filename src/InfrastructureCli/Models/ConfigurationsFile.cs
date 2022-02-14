using System.Collections.Generic;
using System.Text.Json;

namespace InfrastructureCli.Models
{
    public record ConfigurationsFile
    (
        Dictionary<string, Configuration> Configurations
    );
}
