using System.Collections.Generic;
using System.Text.Json;

namespace InfrastructureCli.Rewriters;

internal sealed class MapPropertiesRewriter : RewriterBase, IRewriter
{
    public JsonElement Rewrite(JsonElement jsonElement, IRootRewriter rootRewriter)
    {
        if (TryGetArguments(jsonElement, "MapProperties", out var argumentsElement) != true ||
            argumentsElement.ValueKind != JsonValueKind.Array ||
            argumentsElement.GetArrayLength() != 2 ||
            TryGetProperties(argumentsElement[0], out var childJsonProperties) != true)
        {
            return jsonElement;
        }

        var templateJsonElement = argumentsElement[1];
            
        return BuildJsonElement(jsonWriter =>
        {
            jsonWriter.WriteStartArray();

            foreach (var childJsonProperty in childJsonProperties)
            {
                var attributes = new Dictionary<string, dynamic>
                {
                    ["PropertyKey"] = childJsonProperty.Name,
                    ["PropertyValue"] = childJsonProperty.Value
                };

                rootRewriter
                    .PrependToBottomUp(new GetAttributeValueRewriter<dynamic>(attributes))
                    .Rewrite(templateJsonElement)
                    .WriteTo(jsonWriter);
            }

            jsonWriter.WriteEndArray();
        });
    }
}