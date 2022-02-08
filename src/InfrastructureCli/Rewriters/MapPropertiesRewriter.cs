using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using InfrastructureCli.Extensions;

namespace InfrastructureCli.Rewriters
{
    internal sealed class MapPropertiesRewriter : RewriterBase
    {
        protected override JsonElement RewriteObject(IReadOnlyDictionary<string, JsonElement> jsonProperties, IRewriter rootRewriter)
        {
            if (TryGetArgumentsElement(jsonProperties, "MapProperties", out var mapPropertiesArgumentsElement) != true ||
                mapPropertiesArgumentsElement.ValueKind != JsonValueKind.Array ||
                mapPropertiesArgumentsElement.GetArrayLength() != 2 ||
                mapPropertiesArgumentsElement[0].ValueKind != JsonValueKind.Object)
            {
                return base.RewriteObject(jsonProperties, rootRewriter);
            }
            
            var template = mapPropertiesArgumentsElement[1];
            
            var jsonElements = mapPropertiesArgumentsElement[0]
                .EnumerateObject()
                .Select(property =>
                {
                    var attributes = new Dictionary<string, dynamic>
                    {
                        ["PropertyKey"] = property.Name,
                        ["PropertyValue"] = property.Value,
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
