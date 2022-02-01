using System;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

namespace InfrastructureCli.Commands
{
    internal class InteractiveCommand
    {
        public static void Attach(RootCommand rootCommand)
        {
            var interactiveCommand = new Command("interactive")
            {
                Handler = CommandHandler.Create(async (IConsole console) =>
                {
                    var lastErrorCode = 0;

                    while (true)
                    {
                        console.Out.Write("> ");

                        var input = Console.ReadLine()?.Trim() ?? "";

                        if (input == "")
                        {
                            break;
                        }

                        var args = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        try
                        {
                            lastErrorCode = await rootCommand.InvokeAsync(args, console);
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
