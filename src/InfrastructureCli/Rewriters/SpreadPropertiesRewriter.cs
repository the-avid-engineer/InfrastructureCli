using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace InfrastructureCli.Rewriters
{
    internal sealed class SpreadPropertiesRewriter : RewriterBase
    {
        protected override JsonElement RewriteObject(IReadOnlyDictionary<string, JsonElement> jsonProperties, IRewriter rootRewriter)
        {
            if (TryGetArgumentsElement(jsonProperties, "SpreadProperties", out var spreadPropertiesArgumentsElement) != true ||
                spreadPropertiesArgumentsElement.ValueKind != JsonValueKind.Array)
            {
                return base.RewriteObject(jsonProperties, rootRewriter);
            }

            var allParentElements = spreadPropertiesArgumentsElement.EnumerateArray();

            if (allParentElements.Any(parentElement => parentElement.ValueKind != JsonValueKind.Object))
            {
                return base.RewriteObject(jsonProperties, rootRewriter);
            }
            
            var allJsonProperties = allParentElements
                .SelectMany(jsonElement => jsonElement.EnumerateObject())
                .ToArray();

            return Rewrite(jsonWriter =>
            {
                jsonWriter.WriteStartObject();

                foreach (var jsonProperty in allJsonProperties)
                {
                    jsonProperty.WriteTo(jsonWriter);
                }

                jsonWriter.WriteEndObject();
            });
        }
    }
}