using System.Collections.Generic;
using System.Text.Json;

namespace InfrastructureCli.Models;

public record Configuration
(
    Dictionary<string, Dictionary<string, JsonElement>> RegionAttributes,
    Dictionary<string, JsonElement> Attributes,
    TemplateType TemplateType,
    Dictionary<string, JsonElement> TemplateOptions,
    JsonElement Template
);