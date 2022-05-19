using System.Linq;
using System.Text.Json;

namespace InfrastructureCli.Rewriters;

internal sealed class GetPropertyValueRewriter : RewriterBase, IRewriter
{
    public JsonElement Rewrite(JsonElement jsonElement, IRootRewriter rootRewriter)
    {
        if (TryGetArguments(jsonElement, "GetPropertyValue", out var argumentsElement) != true ||
            argumentsElement.ValueKind != JsonValueKind.Array ||
            argumentsElement.GetArrayLength() != 2 ||
            TryGetProperties(argumentsElement[0], out var childJsonProperties) != true ||
            argumentsElement[1].ValueKind != JsonValueKind.String)
        {
            return jsonElement;
        }
            
        var properties = childJsonProperties
            .ToDictionary(property => property.Name, property => property.Value);

        var propertyName = argumentsElement[1].GetString()!;

        return properties.TryGetValue(propertyName, out var propertyValueElement)
            ? propertyValueElement
            : BuildJsonElement(jsonWriter =>
            {
                jsonWriter.WriteStartObject();
                jsonWriter.WritePropertyName("@Fn::Error");
                jsonWriter.WriteStringValue($"Unknown Property Key: {propertyName}");
                jsonWriter.WriteEndObject();
            });
    }
}