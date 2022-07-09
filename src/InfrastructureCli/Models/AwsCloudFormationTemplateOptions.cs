using System.Collections.Generic;

namespace InfrastructureCli.Models;

public record AwsCloudFormationTemplateOptions
(
    string? StackName = null,
    bool? UseChangeSet = null,
    string[]? Capabilities = null,
    Dictionary<string, string>? Tags = null
);
