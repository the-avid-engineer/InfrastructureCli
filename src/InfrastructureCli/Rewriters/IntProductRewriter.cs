using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace InfrastructureCli.Rewriters
{
    internal sealed class MultiplyRewriter : RewriterBase
    {
        protected override JsonElement RewriteObject(IReadOnlyDictionary<string, JsonElement> jsonProperties, IRewriter rootRewriter)
        {
            if (jsonProperties.Count != 1 ||
                jsonProperties.TryGetValue("@IntProduct", out var mapPropertiesBodyElement) != true ||
                mapPropertiesBodyElement.ValueKind != JsonValueKind.Array)
            {
                return base.RewriteObject(jsonProperties, rootRewriter);
            }

            var allElements = mapPropertiesBodyElement.EnumerateArray().ToArray();

            if (allElements.Any(element => element.ValueKind != JsonValueKind.Number))
            {
                return base.RewriteObject(jsonProperties, rootRewriter);
            }

            var product = allElements
                .Select(element => element.GetInt32())
                .Aggregate(1, (x, y) => x * y);
            
            return Rewrite(jsonWriter =>
            {
                jsonWriter.WriteNumberValue(product);
            });
        }
    }
}