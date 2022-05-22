using System.Collections.Generic;

namespace InfrastructureCli.Models;

public record AwsCloudFormationTemplateOptions
(
    string? StackName,
    bool? UseChangeSet,
    string[]? Capabilities,
    Dictionary<string, string>? Tags
);
