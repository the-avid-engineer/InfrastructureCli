using System.Linq;
using System.Text.Json;

namespace InfrastructureCli.Rewriters;

internal class UsingAttributesRewriter : RewriterBase, IRewriter
{
    public JsonElement Rewrite(JsonElement jsonElement, IRootRewriter rootRewriter)
    {
        if (TryGetArguments(jsonElement, "UsingAttributes", out var argumentsElement) != true ||
            argumentsElement.ValueKind != JsonValueKind.Array ||
            argumentsElement.GetArrayLength() != 2 ||
            TryGetProperties(argumentsElement[0], out var childJsonProperties) != true)
        {
            return jsonElement;
        }
            
        var attributes = childJsonProperties
            .ToDictionary(property => property.Name, property => property.Value);
            
        var templateJsonElement = argumentsElement[1];

        return rootRewriter
            .PrependToBottomUp(new GetAttributeValueRewriter<JsonElement>(attributes))
            .PrependToBottomUp(new AttributeValueDefinedRewriter(attributes.Keys))
            .Rewrite(templateJsonElement);
    }
}