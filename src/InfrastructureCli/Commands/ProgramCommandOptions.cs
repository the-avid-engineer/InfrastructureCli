namespace InfrastructureCli.Commands;

public record ProgramCommandOptions
(
    IGenerateCommand[] GenerateCommands,
    IValidateConfigurationsFile? ValidateConfigurationsFile = null
);