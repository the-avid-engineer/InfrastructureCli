﻿using System.Collections.Generic;
using System.Text.Json;

namespace InfrastructureCli.Models
{
    public record Configuration
    (
        string Label,
        Dictionary<string, JsonElement> Attributes,
        Dictionary<string, string> Tags,
        Dictionary<string, string> PropertyMaps
    );
}
