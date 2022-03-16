using System.Collections.Generic;
using System.Text.Json;

namespace InfrastructureCli.Models;

internal record DeployOptions
(
    Configuration Configuration,
    JsonElement Template,
    bool UsePreviousParameters,
    Dictionary<string, string> Parameters
);