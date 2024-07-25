using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using InfrastructureCli.Models;
using InfrastructureCli.Services;

namespace InfrastructureCli.Commands;

public abstract record GenerateConfigurationCommandBase<TArguments> : CommandBase, IChildCommand
    where TArguments : IGenerateConfigurationCommandArguments
{
    protected virtual async Task Execute(TArguments arguments)
    {
        var configurationKey = GetConfigurationKey(arguments);
        var configuration = await GetConfiguration(arguments);

        var configurationsFile = await LoadOrCreateConfigurationsFile(arguments.ConfigurationsFileName);

        if (configurationsFile.Configurations.ContainsKey(configurationKey))
        {
            configurationsFile.Configurations[configurationKey] = configuration;
        }
        else
        {
            configurationsFile.Configurations.Add(configurationKey, configuration);
        }

        await FileService.SerializeToFile(configurationsFile, arguments.ConfigurationsFileName);
    }

    protected abstract Command GetCommand();
    protected abstract string GetConfigurationKey(TArguments arguments);
    protected abstract Task<Configuration> GetConfiguration(TArguments arguments);

    private static async Task<ConfigurationsFile> LoadOrCreateConfigurationsFile(FileInfo configurationsFileName)
    {
        if (!configurationsFileName.Exists)
        {
            return new ConfigurationsFile
            (
                new Dictionary<string, Dictionary<string, JsonElement>>(),
                new Dictionary<string, JsonElement>(),
                new Dictionary<string, Configuration>()
            );
        }

        await using var configurationsFileStream = configurationsFileName.OpenRead();

        return await JsonService.DeserializeAsync<ConfigurationsFile>(configurationsFileStream);
    }

    public void Attach(Command parentCommand)
    {
        var command = GetCommand();

        command.Handler = CommandHandler.Create<TArguments>(Execute);

        AttachConfigurationsFileNameOption(command);

        parentCommand.AddCommand(command);
    }
}