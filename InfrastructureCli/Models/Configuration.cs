using System.Collections.Generic;

namespace InfrastructureCli.Models
{
    public record Configuration
    (
        string Label,
        Dictionary<string, string> Parameters,
        Dictionary<string, string> Tags,
        Dictionary<string, string> Metas
    );
}
