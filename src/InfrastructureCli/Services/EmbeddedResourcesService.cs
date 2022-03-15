using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InfrastructureCli.Services;

/// <summary>
///     Provides a method for writing template files that are embedded as resources.
/// </summary>
public static class EmbeddedResourcesService
{
    /// <summary>
    ///     Copy embedded resources to a specified directory.
    /// </summary>
    /// <param name="resourceFilesDirectory">The base directory where resources will be written to.</param>
    /// <param name="embeddedResourceAssembly">The assembly containing the embedded resources.</param>
    /// <param name="embeddedResourceNamespaces">The namespaces of the embedded resources that should be copied over.</param>
    public static async Task Copy
    (
        DirectoryInfo resourceFilesDirectory,
        Assembly embeddedResourceAssembly,
        params string[] embeddedResourceNamespaces
    )
    {
        foreach (var embeddedResourceNamespace in embeddedResourceNamespaces)
        {
            var embeddedResourceFullNames = embeddedResourceAssembly
                .GetManifestResourceNames()
                .Where(embeddedResourceFullName => embeddedResourceFullName.StartsWith(embeddedResourceNamespace));

            foreach (var embeddedResourceFullName in embeddedResourceFullNames)
            {
                var embeddedResourceName = embeddedResourceFullName[embeddedResourceNamespace.Length..];
                    
                var numberOfPathComponents = Regex.Matches(embeddedResourceName, Regex.Escape(".")).Count;

                var outputResourceFilePathComponents = embeddedResourceName
                    .Split(".", numberOfPathComponents)
                    .Prepend(resourceFilesDirectory.FullName)
                    .ToArray();
                    
                var outputResourceFilePath = Path.Combine(outputResourceFilePathComponents);
                    
                var outputResourceFileName = new FileInfo(outputResourceFilePath);
                    
                await using var embeddedResourceStream = embeddedResourceAssembly.GetManifestResourceStream(embeddedResourceFullName);

                await FileService.WriteToFile(embeddedResourceStream!, outputResourceFileName);
            }
        }
    }
}