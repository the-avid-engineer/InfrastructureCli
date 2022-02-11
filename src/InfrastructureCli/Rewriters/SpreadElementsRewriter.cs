using System.Linq;
using System.Text.Json;

namespace InfrastructureCli.Rewriters
{
    internal sealed class SpreadElementsRewriter : RewriterBase, IRewriter
    {
        public JsonElement Rewrite(JsonElement jsonElement, IRewriter rootRewriter)
        {
            if (TryGetArguments(jsonElement, "SpreadElements", out var argumentsElement) != true ||
                argumentsElement.ValueKind != JsonValueKind.Array)
            {
                return jsonElement;
            }

            var childJsonElements = argumentsElement
                .EnumerateArray()
                .ToArray();

            if (childJsonElements.Any(childJsonElement => childJsonElement.ValueKind != JsonValueKind.Array))
            {
                return jsonElement;
            }
            
            var allChildJsonElements = childJsonElements
                .SelectMany(childJsonElement => childJsonElement.EnumerateArray())
                .ToArray();

            return BuildJsonElement(jsonWriter =>
            {
                jsonWriter.WriteStartArray();

                foreach (var childJsonElement in allChildJsonElements)
                {
                    childJsonElement.WriteTo(jsonWriter);
                }

                jsonWriter.WriteEndArray();
            });
        }
    }
}
