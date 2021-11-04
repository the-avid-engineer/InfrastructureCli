using System.Collections.Generic;
using System.Text.Json;

namespace InfrastructureCli.Models
{
    internal record DeployOptions
    (
        Configuration Configuration,
        bool UsePreviousTemplate,
        JsonElement Template,
        bool UsePreviousParameters,
        Dictionary<string, string> Parameters
    );
}