using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace InfrastructureCli.Rewriters;

internal sealed class MapNamedElementsRewriter : RewriterBase, IRewriter
{
    public JsonElement Rewrite(JsonElement jsonElement, IRootRewriter rootRewriter)
    {
        if (TryGetArguments(jsonElement, "MapNamedElements", out var argumentsElement) != true ||
            argumentsElement.ValueKind != JsonValueKind.Array ||
            argumentsElement.GetArrayLength() != 3 ||
            argumentsElement[0].ValueKind != JsonValueKind.Array ||
            argumentsElement[1].ValueKind != JsonValueKind.String)
        {
            return jsonElement;
        }

        var childJsonElements = argumentsElement[0]
            .EnumerateArray()
            .ToArray();

        var elementName = argumentsElement[1].GetString();

        var templateJsonElements = argumentsElement[2];
            
        return BuildJsonElement(jsonWriter =>
        {
            jsonWriter.WriteStartArray();

            for (var i = 0; i < childJsonElements.Length; i++)
            {
                var attributes = new Dictionary<string, dynamic>
                {
                    [$"{elementName}Index"] = i,
                    [$"{elementName}Value"] = childJsonElements[i]
                };

                rootRewriter
                    .PrependToBottomUp(new GetAttributeValueRewriter<dynamic>(attributes))
                    .Rewrite(templateJsonElements)
                    .WriteTo(jsonWriter);
            }

            jsonWriter.WriteEndArray();
        });
    }
}