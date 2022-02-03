using System.Collections.Generic;
using System.Text.Json;

namespace InfrastructureCli.Models
{
    public record ConfigurationsFile
    (
        TemplateType TemplateType,
        Dictionary<string, JsonElement> TemplateOptions, 
        Dictionary<string, Configuration> Configurations
    );
}
