using System.Collections.Generic;
using System.Text.Json;

namespace InfrastructureCli.Models;

internal record GetOptions
(
    IReadOnlyDictionary<string, JsonElement> TemplateOptions,
    string PropertyName
);