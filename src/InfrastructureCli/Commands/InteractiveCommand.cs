using System;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

namespace InfrastructureCli.Commands
{
    internal class InteractiveCommand : CommandBase
    {
        public static void Attach(RootCommand rootCommand)
        {
            var interactiveCommand = new Command("interactive")
            {
                Handler = CommandHandler.Create(async (IConsole console) =>
                {
                    var lastErrorCode = 0;

                    var lastArgs = Array.Empty<string>();

                    while (true)
                    {
                        console.Out.Write("> ");

                        var input = Console.ReadLine()?.Trim() ?? "";

                        if (input == "")
                        {
                            break;
                        }

                        if (input != "repeat")
                        {
                            lastArgs = input.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                        }

                        try
                        {
                            lastErrorCode = await rootCommand.InvokeAsync(lastArgs, console);
                        }
                        catch (Exception exception)
                        {
                            console.Out.Write(exception.ToString());
                            lastErrorCode = -1;
                        }

                        console.Out.Write(Environment.NewLine);
                    }

                    return lastErrorCode;
                })
            };
            
            rootCommand.AddCommand(interactiveCommand);
        }
    }
}
