using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace InfrastructureCli.Rewriters
{
    internal sealed class SpreadElementsRewriter : RewriterBase
    {
        protected override JsonElement RewriteObject(IReadOnlyDictionary<string, JsonElement> jsonProperties, IRewriter rootRewriter)
        {
            if (TryGetArgumentsElement(jsonProperties, "SpreadElements", out var spreadElementsArgumentsElement) != true ||
                spreadElementsArgumentsElement.ValueKind != JsonValueKind.Array)
            {
                return base.RewriteObject(jsonProperties, rootRewriter);
            }

            var allParentElements = spreadElementsArgumentsElement.EnumerateArray();

            if (allParentElements.Any(parentElement => parentElement.ValueKind != JsonValueKind.Array))
            {
                return base.RewriteObject(jsonProperties, rootRewriter);
            }
            
            var allJsonElements = allParentElements
                .SelectMany(jsonElement => jsonElement.EnumerateArray())
                .ToArray();

            return Rewrite(jsonWriter =>
            {
                jsonWriter.WriteStartArray();

                foreach (var jsonElement in allJsonElements)
                {
                    jsonElement.WriteTo(jsonWriter);
                }

                jsonWriter.WriteEndArray();
            });
        }
    }
}
