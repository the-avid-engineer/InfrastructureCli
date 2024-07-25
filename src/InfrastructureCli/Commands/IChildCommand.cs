using System.CommandLine;

namespace InfrastructureCli.Commands;

public interface IChildCommand
{
    void Attach(Command parentCommand); 
}