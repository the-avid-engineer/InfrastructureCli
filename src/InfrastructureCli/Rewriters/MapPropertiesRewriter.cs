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
            if (jsonProperties.Count != 1 ||
                jsonProperties.TryGetValue("@MapProperties", out var mapPropertiesBodyElement) != true ||
                mapPropertiesBodyElement.ValueKind != JsonValueKind.Array ||
                mapPropertiesBodyElement.GetArrayLength() != 2 ||
                mapPropertiesBodyElement[0].ValueKind != JsonValueKind.Object)
            {
                return base.RewriteObject(jsonProperties, rootRewriter);
            }
            
            var template = mapPropertiesBodyElement[1];
            
            var jsonElements = mapPropertiesBodyElement[0]
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
