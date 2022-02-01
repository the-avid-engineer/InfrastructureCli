using System.CommandLine;

namespace InfrastructureCli.Commands
{
    public interface IGenerateCommand
    {
        void Attach(Command parentCommand);
    }
}
