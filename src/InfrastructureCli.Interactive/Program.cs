﻿using System;
using System.Threading.Tasks;
using InfrastructureCli.Commands;

namespace InfrastructureCli.Interactive;

public static class Program
{
    public static Task<int> Main()
    {
        return new ProgramCommand(new ProgramCommandOptions(Array.Empty<IGenerateCommand>())).Invoke(new[]{"interactive"});
    }
}