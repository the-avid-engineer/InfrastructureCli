using System.IO;
using System.Linq;
using System.Text.Json;
using InfrastructureCli.Extensions;
using InfrastructureCli.Services;

namespace InfrastructureCli.Rewriters;

internal sealed class IncludeFileRewriter : RewriterBase, IRewriter
{
    private readonly string _currentPath;

    public IncludeFileRewriter(string currentPath)
    {
        _currentPath = currentPath;
    }
        
    public JsonElement Rewrite(JsonElement jsonElement, IRewriter rootRewriter)
    {
        if (TryGetArguments(jsonElement, "IncludeFile", out var argumentsElement) != true ||
            argumentsElement.ValueKind != JsonValueKind.Array)
        {
            return jsonElement;
        }

        var childJsonElements = argumentsElement
            .EnumerateArray()
            .ToArray();

        if (childJsonElements.Any(childJsonElement => childJsonElement.ValueKind != JsonValueKind.String))
        {
            return jsonElement;
        }

        var fileNameComponents = childJsonElements
            .Select(childJsonElement => childJsonElement.GetString()!)
            .Prepend(_currentPath)
            .ToArray();

        var fileName = Path.Combine(fileNameComponents);
            
        var fileInfo = new FileInfo(fileName);

        var newJsonElement = FileService.DeserializeFromFile<JsonElement>(fileInfo).Result;

        var augmentedRewriter = new ChainRewriter
        (
            ChainRewriter.ForCurrentPath(fileInfo.DirectoryName!),
            rootRewriter
        );

        return augmentedRewriter.Rewrite(newJsonElement);
    }
}