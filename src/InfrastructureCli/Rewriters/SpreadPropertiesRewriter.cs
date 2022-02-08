using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace InfrastructureCli.Rewriters
{
    internal sealed class SpreadPropertiesRewriter : RewriterBase
    {
        protected override JsonElement RewriteObject(IReadOnlyDictionary<string, JsonElement> jsonProperties, IRewriter rootRewriter)
        {
            if (jsonProperties.Count != 1 ||
                jsonProperties.TryGetValue("@SpreadProperties", out var spreadPropertiesBodyElement) != true ||
                spreadPropertiesBodyElement.ValueKind != JsonValueKind.Array)
            {
                return base.RewriteObject(jsonProperties, rootRewriter);
            }

            var allParentElements = spreadPropertiesBodyElement.EnumerateArray();

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