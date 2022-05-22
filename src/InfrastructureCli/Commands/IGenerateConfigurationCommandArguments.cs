using System.IO;

namespace InfrastructureCli.Commands;

public interface IGenerateConfigurationCommandArguments
{
    FileInfo ConfigurationsFileName { get; }
}
