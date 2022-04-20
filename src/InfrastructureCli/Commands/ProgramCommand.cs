using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;

namespace InfrastructureCli.Commands;

public class ProgramCommand
{
    private readonly RootCommand _rootCommand;

    [Obsolete("This constructor will be removed in the future. Pass in a ProgramCommandOptions object instead.")]
    public ProgramCommand(IEnumerable<IGenerateCommand> generateCommands) : this(new ProgramCommandOptions(generateCommands.ToArray()))
    {
    }
    
    public ProgramCommand(ProgramCommandOptions options)
    {
        var rootCommand = new RootCommand();

        InteractiveCommand.Attach(rootCommand);
        NewCommand.Attach(rootCommand, options.GenerateCommands);
        CanDeployCommand.Attach(rootCommand);
        DeployCommand.Attach(rootCommand, options.ValidateConfigurationsFile);
        GetCommand.Attach(rootCommand);

        _rootCommand = rootCommand;
    }

    public Task<int> Invoke(string[] args)
    {
        return _rootCommand.InvokeAsync(args);
    }
}