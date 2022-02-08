using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using InfrastructureCli.Extensions;

namespace InfrastructureCli.Rewriters
{
    internal sealed class MapElementsRewriter : RewriterBase
    {
        protected override JsonElement RewriteObject(IReadOnlyDictionary<string, JsonElement> jsonProperties, IRewriter rootRewriter)
        {
            if (TryGetArgumentsElement(jsonProperties, "MapElements", out var mapElementsArgumentsElement) != true ||
                mapElementsArgumentsElement.ValueKind != JsonValueKind.Array ||
                mapElementsArgumentsElement.GetArrayLength() != 2 ||
                mapElementsArgumentsElement[0].ValueKind != JsonValueKind.Array)
            {
                return base.RewriteObject(jsonProperties, rootRewriter);
            }
            
            var template = mapElementsArgumentsElement[1];
            
            var jsonElements = mapElementsArgumentsElement[0]
                .EnumerateArray()
                .Select((value, index) =>
                {
                    var attributes = new Dictionary<string, dynamic>
                    {
                        ["ElementIndex"] = index,
                        ["ElementValue"] = value,
                    };

                    var augmentedRootRewriter = new ChainRewriter
                    (
                        new GetAttributeValueRewriter<dynamic>(attributes),
                        rootRewriter
                    );

                    return augmentedRootRewriter.Rewrite(template);
                });
            
            return Rewrite(jsonWriter =>
            {
                jsonWriter.WriteStartArray();

                foreach (var jsonElement in jsonElements)
                {
                    jsonElement.WriteTo(jsonWriter);
                }

                jsonWriter.WriteEndArray();
            });
        }
    }
}
