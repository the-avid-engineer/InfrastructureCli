using System.Linq;
using System.Text.Json;

namespace InfrastructureCli.Rewriters;

internal sealed class IntProductionRewriter : RewriterBase, IRewriter
{
    public JsonElement Rewrite(JsonElement jsonElement, IRewriter rootRewriter)
    {
        if (TryGetArguments(jsonElement, "IntProduct", out var argumentsElement) != true ||
            argumentsElement.ValueKind != JsonValueKind.Array)
        {
            return jsonElement;
        }

        var childJsonElements = argumentsElement
            .EnumerateArray()
            .ToArray();

        if (childJsonElements.Any(childJsonElement => childJsonElement.ValueKind != JsonValueKind.Number))
        {
            return jsonElement;
        }

        var product = childJsonElements
            .Select(element => element.GetInt32())
            .Aggregate(1, (x, y) => x * y);
            
        return BuildJsonElement(jsonWriter =>
        {
            jsonWriter.WriteNumberValue(product);
        });
    }
}