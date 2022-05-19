using System.IO;
using System.Linq;
using System.Text.Json;
using InfrastructureCli.Services;

namespace InfrastructureCli.Rewriters;

internal sealed class IncludeRawFileRewriter : RewriterBase, IRewriter
{
    public JsonElement Rewrite(JsonElement jsonElement, IRootRewriter rootRewriter)
    {
        if (TryGetArguments(jsonElement, "IncludeRawFile", out var argumentsElement) != true ||
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
            .Prepend(rootRewriter.CurrentPath)
            .ToArray();

        var fileName = Path.Combine(fileNameComponents);
            
        var fileInfo = new FileInfo(fileName);

        var rawFile = FileService.GetRawFile(fileInfo).Result;

        return JsonService.Convert<string, JsonElement>(rawFile);
    }
}