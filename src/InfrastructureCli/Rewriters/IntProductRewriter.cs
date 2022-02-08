using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace InfrastructureCli.Rewriters
{
    internal sealed class IntProductionRewriter : RewriterBase
    {
        protected override JsonElement RewriteObject(IReadOnlyDictionary<string, JsonElement> jsonProperties, IRewriter rootRewriter)
        {
            if (TryGetArgumentsElement(jsonProperties, "IntProduct", out var getIntProductArgumentsElement) != true ||
                getIntProductArgumentsElement.ValueKind != JsonValueKind.Array)
            {
                return base.RewriteObject(jsonProperties, rootRewriter);
            }

            var allElements = getIntProductArgumentsElement.EnumerateArray().ToArray();

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