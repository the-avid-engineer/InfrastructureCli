using System;
using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;

namespace InfrastructureCli.Services
{
    public static class OptionService
    {
        private static readonly string DefaultDirectory = Environment.CurrentDirectory;

        public static Dictionary<string, string> ParseDictionary(ArgumentResult argumentResult)
        {
            return argumentResult.Tokens
                .Select(token => token.Value.Split('=', 2))
                .ToDictionary(pieces => pieces[0], p => p[1]);
        }

        public static DirectoryInfo DefaultOutputsDirectoryName() => new(DefaultDirectory);

        public static FileInfo DefaultConfigurationsFileName(string? directory = null) =>
            new(Path.Combine(directory ?? DefaultDirectory, "configurations.json"));

        public static string? DefaultEnvironmentVariable(string environmentVariableName) =>
            Environment.GetEnvironmentVariable(environmentVariableName);
    }
}
