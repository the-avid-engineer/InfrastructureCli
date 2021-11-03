using System;
using System.IO;

namespace InfrastructureCli.Services
{
    public static class OptionService
    {
        private static readonly string DefaultDirectory = Environment.CurrentDirectory;

        public static DirectoryInfo DefaultOutputsDirectoryName() => new(DefaultDirectory);

        public static FileInfo DefaultConfigurationsFileName(string? directory = null) =>
            new(Path.Combine(directory ?? DefaultDirectory, "configurations.json"));

        public static FileInfo DefaultTemplateFileName(string? directory = null) =>
            new(Path.Combine(directory ?? DefaultDirectory, "template.json"));

        public static string? DefaultEnvironmentVariable(string environmentVariableName) =>
            Environment.GetEnvironmentVariable(environmentVariableName);
    }
}
