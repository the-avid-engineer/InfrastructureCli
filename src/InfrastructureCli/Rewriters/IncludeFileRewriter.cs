using System.IO;
using System.Linq;
using System.Text.Json;
using InfrastructureCli.Services;

namespace InfrastructureCli.Rewriters;

internal sealed class IncludeFileRewriter : RewriterBase, IRewriter
{
    public JsonElement Rewrite(JsonElement jsonElement, IRootRewriter rootRewriter)
    {
        if (TryGetArguments(jsonElement, "IncludeFile", out var argumentsElement) != true ||
            argumentsElement.ValueKind != JsonValueKind.Array)
        {
            return jsonElement;
        }

        var childJsonElements = argumentsElement
            .EnumerateArray()
            .ToArray();

        var nonStringChildJsonElements = childJsonElements
            .Where(childJsonElement => childJsonElement.ValueKind != JsonValueKind.String)
            .ToArray();

        if (nonStringChildJsonElements.Any())
        {
            if (nonStringChildJsonElements.All(childJsonElements => childJsonElements.ValueKind == JsonValueKind.Object && !IsNormalObject(childJsonElements)))
            {
                return BuildJsonElement((jsonWriter) =>
                {
                    jsonWriter.WriteStartObject();
                    jsonWriter.WritePropertyName("@Fn::IncludeFileFromPath");
                    jsonWriter.WriteStartArray();

                    jsonWriter.WriteStringValue(rootRewriter.CurrentPath);

                    foreach (var childJsonElement in childJsonElements)
                    {
                        childJsonElement.WriteTo(jsonWriter);
                    }

                    jsonWriter.WriteEndArray();
                    jsonWriter.WriteEndObject();
                });
            }

            return jsonElement;
        }

        var fileNameComponents = childJsonElements
            .Select(childJsonElement => childJsonElement.GetString()!)
            .Prepend(rootRewriter.CurrentPath)
            .ToArray();

        var fileName = Path.Combine(fileNameComponents);
            
        var fileInfo = new FileInfo(fileName);

        var newJsonElement = FileService.DeserializeFromFile<JsonElement>(fileInfo).Result;

        return rootRewriter
            .WithCurrentPath(fileInfo.DirectoryName!)
            .Rewrite(newJsonElement);
    }
}