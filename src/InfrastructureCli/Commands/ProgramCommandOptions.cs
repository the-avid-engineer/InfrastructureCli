namespace InfrastructureCli.Commands;

public record ProgramCommandOptions
{
    public IChildCommand[] GenerateCommands { get; init; } = Array.Empty<IChildCommand>();
    public IChildCommand[] CustomCommands { get; init; } = Array.Empty<IChildCommand>();
    public IValidateConfigurationsFile? ValidateConfigurationsFile { get; init; }
}