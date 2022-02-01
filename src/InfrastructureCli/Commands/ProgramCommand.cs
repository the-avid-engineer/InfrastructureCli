using System.Collections.Generic;
using System.CommandLine;
using System.Threading.Tasks;

namespace InfrastructureCli.Commands
{
    public class ProgramCommand
    {
        private readonly RootCommand _rootCommand;

        public ProgramCommand(IEnumerable<IGenerateCommand> generateCommands)
        {
            var rootCommand = new RootCommand();

            InteractiveCommand.Attach(rootCommand);
            NewCommand.Attach(rootCommand, generateCommands);
            GetCommand.Attach(rootCommand);
            DeployCommand.Attach(rootCommand);

            _rootCommand = rootCommand;
        }

        public Task<int> Invoke(string[] args)
        {
            return _rootCommand.InvokeAsync(args);
        }
    }
}
