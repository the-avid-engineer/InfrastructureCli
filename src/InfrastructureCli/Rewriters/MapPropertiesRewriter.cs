using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using InfrastructureCli.Extensions;

namespace InfrastructureCli.Rewriters
{
    internal sealed class MapPropertiesRewriter : RewriterBase, IRewriter
    {
        public JsonElement Rewrite(JsonElement jsonElement, IRewriter rootRewriter)
        {
            if (TryGetArguments(jsonElement, "MapProperties", out var argumentsElement) != true ||
                argumentsElement.ValueKind != JsonValueKind.Array ||
                argumentsElement.GetArrayLength() != 2 ||
                TryGetProperties(argumentsElement[0], out var childJsonProperties) != true)
            {
                return jsonElement;
            }

            var templateJsonElement = argumentsElement[1];
            
            return BuildJsonElement(jsonWriter =>
            {
                jsonWriter.WriteStartArray();

                foreach (var childJsonProperty in childJsonProperties)
                {
                    var attributes = new Dictionary<string, dynamic>
                    {
                        ["PropertyKey"] = childJsonProperty.Name,
                        ["PropertyValue"] = childJsonProperty.Value,
                    };

                    var augmentedRewriter = new ChainRewriter
                    (
                        new GetAttributeValueRewriter<dynamic>(attributes),
                        rootRewriter
                    );

                    augmentedRewriter.Rewrite(templateJsonElement).WriteTo(jsonWriter);
                }

                jsonWriter.WriteEndArray();
            });
        }
    }
}
