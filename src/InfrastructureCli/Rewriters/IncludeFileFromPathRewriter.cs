using System.IO;
using System.Linq;
using System.Text.Json;
using InfrastructureCli.Services;

namespace InfrastructureCli.Rewriters;

internal sealed class IncludeFileFromPathRewriter : RewriterBase, IRewriter
{
    public JsonElement Rewrite(JsonElement jsonElement, IRootRewriter rootRewriter)
    {
        if (TryGetArguments(jsonElement, "IncludeFileFromPath", out var argumentsElement) != true ||
            argumentsElement.ValueKind != JsonValueKind.Array ||
            argumentsElement.GetArrayLength() < 2)
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
            .ToArray();

        var fileName = Path.Combine(fileNameComponents);
            
        var fileInfo = new FileInfo(fileName);

        var newJsonElement = FileService.DeserializeFromFile<JsonElement>(fileInfo).Result;

        return rootRewriter
            .WithCurrentPath(fileInfo.DirectoryName!)
            .Rewrite(newJsonElement);
    }
}