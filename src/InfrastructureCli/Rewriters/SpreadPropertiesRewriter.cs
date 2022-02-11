using System.Linq;
using System.Text.Json;
using InfrastructureCli.Extensions;

namespace InfrastructureCli.Rewriters
{
    internal sealed class SpreadPropertiesRewriter : RewriterBase, IRewriter
    {
        public JsonElement Rewrite(JsonElement jsonElement, IRewriter rootRewriter)
        {
            if (TryGetArguments(jsonElement, "SpreadProperties", out var argumentsElement) != true ||
                argumentsElement.ValueKind != JsonValueKind.Array)
            {
                return jsonElement;
            }

            var childJsonElements = argumentsElement
                .EnumerateArray()
                .ToArray();

            if (childJsonElements.Any(childJsonElement => IsNormalObject(childJsonElement) != true))
            {
                return jsonElement;
            }
            
            var allChildJsonProperties = childJsonElements
                .SelectMany(childJsonElement => childJsonElement.EnumerateObject())
                .ToArray();

            return BuildJsonElement(jsonWriter =>
            {
                jsonWriter.WriteStartObject();

                foreach (var childJsonProperty in allChildJsonProperties)
                {
                    childJsonProperty.WriteTo(jsonWriter);
                }

                jsonWriter.WriteEndObject();
            });
        }
    }
}