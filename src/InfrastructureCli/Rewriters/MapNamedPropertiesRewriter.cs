using System.Collections.Generic;
using System.Text.Json;

namespace InfrastructureCli.Rewriters;

internal sealed class MapNamedPropertiesRewriter : RewriterBase, IRewriter
{
    public JsonElement Rewrite(JsonElement jsonElement, IRootRewriter rootRewriter)
    {
        if (TryGetArguments(jsonElement, "MapNamedProperties", out var argumentsElement) != true ||
            argumentsElement.ValueKind != JsonValueKind.Array ||
            argumentsElement.GetArrayLength() != 3 ||
            TryGetProperties(argumentsElement[0], out var childJsonProperties) != true ||
            argumentsElement[1].ValueKind != JsonValueKind.String)
        {
            return jsonElement;
        }

        var propertyName = argumentsElement[1].GetString();

        var templateJsonElement = argumentsElement[2];
            
        return BuildJsonElement(jsonWriter =>
        {
            jsonWriter.WriteStartArray();

            foreach (var childJsonProperty in childJsonProperties)
            {
                var attributes = new Dictionary<string, dynamic>
                {
                    [$"{propertyName}Key"] = childJsonProperty.Name,
                    [$"{propertyName}Value"] = childJsonProperty.Value
                };

                rootRewriter
                    .PrependToBottomUp(new GetAttributeValueRewriter<dynamic>(attributes))
                    .PrependToBottomUp(new AttributeValueDefinedRewriter(attributes.Keys))
                    .Rewrite(templateJsonElement)
                    .WriteTo(jsonWriter);
            }

            jsonWriter.WriteEndArray();
        });
    }
}