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
            if (jsonProperties.Count != 1 ||
                jsonProperties.TryGetValue("@MapElements", out var mapElementsBodyElement) != true ||
                mapElementsBodyElement.ValueKind != JsonValueKind.Array ||
                mapElementsBodyElement.GetArrayLength() != 2 ||
                mapElementsBodyElement[0].ValueKind != JsonValueKind.Array)
            {
                return base.RewriteObject(jsonProperties, rootRewriter);
            }
            
            var template = mapElementsBodyElement[1];
            
            var jsonElements = mapElementsBodyElement[0]
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
