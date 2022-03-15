using System.Collections.Generic;

namespace InfrastructureCli.Models;

public record ConfigurationsFile
(
    Dictionary<string, Configuration> Configurations
);