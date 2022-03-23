using System.Collections.Generic;
using System.Text.Json;

namespace InfrastructureCli.Models;

public record ConfigurationsFile
(
    Dictionary<string, Dictionary<string, JsonElement>> GlobalRegionAttributes,
    Dictionary<string, JsonElement> GlobalAttributes,
    Dictionary<string, Configuration> Configurations
);