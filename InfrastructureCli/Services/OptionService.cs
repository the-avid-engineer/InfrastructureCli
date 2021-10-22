using System;
using System.IO;

namespace InfrastructureCli.Services
{
    internal static class OptionService
    {
        private static readonly string DefaultDirectory = Environment.CurrentDirectory;

        public static DirectoryInfo DefaultOutputsDirectoryName() => new(DefaultDirectory);

        public static FileInfo DefaultConfigurationFileName(string? directory = null) =>
            new(Path.Combine(directory ?? DefaultDirectory, "configurations.json"));
    }
}
