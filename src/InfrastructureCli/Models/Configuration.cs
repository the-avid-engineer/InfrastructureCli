using System.Collections.Generic;
using System.Text.Json;

namespace InfrastructureCli.Models;

public record Configuration
(
    Dictionary<string, JsonElement> Attributes,
    Dictionary<string, string> PropertyMaps,
    TemplateType TemplateType,
    Dictionary<string, JsonElement> TemplateOptions,
    JsonElement Template
);