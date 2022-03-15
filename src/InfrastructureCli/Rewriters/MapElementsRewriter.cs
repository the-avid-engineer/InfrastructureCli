using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using InfrastructureCli.Extensions;

namespace InfrastructureCli.Rewriters;

internal sealed class MapElementsRewriter : RewriterBase, IRewriter
{
    public JsonElement Rewrite(JsonElement jsonElement, IRewriter rootRewriter)
    {
        if (TryGetArguments(jsonElement, "MapElements", out var argumentsElement) != true ||
            argumentsElement.ValueKind != JsonValueKind.Array ||
            argumentsElement.GetArrayLength() != 2 ||
            argumentsElement[0].ValueKind != JsonValueKind.Array)
        {
            return jsonElement;
        }

        var childJsonElements = argumentsElement[0]
            .EnumerateArray()
            .ToArray();
            
        var templateJsonElements = argumentsElement[1];
            
        return BuildJsonElement(jsonWriter =>
        {
            jsonWriter.WriteStartArray();

            for (var i = 0; i < childJsonElements.Length; i++)
            {
                var attributes = new Dictionary<string, dynamic>
                {
                    ["ElementIndex"] = i,
                    ["ElementValue"] = childJsonElements[i]
                };

                var augmentedRewriter = new ChainRewriter
                (
                    new GetAttributeValueRewriter<dynamic>(attributes),
                    rootRewriter
                );

                augmentedRewriter.Rewrite(templateJsonElements).WriteTo(jsonWriter);
            }

            jsonWriter.WriteEndArray();
        });
    }
}