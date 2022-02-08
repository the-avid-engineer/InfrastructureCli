using System;
using System.Threading.Tasks;
using InfrastructureCli.Commands;

namespace InfrastructureCli.Interactive
{
    public static class Program
    {
        public static Task<int> Main(string[] args)
        {
            return new ProgramCommand(Array.Empty<IGenerateCommand>()).Invoke(args);
        }
    }
}