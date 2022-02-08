using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using InfrastructureCli.Extensions;

namespace InfrastructureCli.Rewriters
{
    internal class UsingAttributesRewriter : RewriterBase
    {
        protected override JsonElement RewriteObject(IReadOnlyDictionary<string, JsonElement> jsonProperties, IRewriter rootRewriter)
        {
            if (jsonProperties.Count != 1 ||
                jsonProperties.TryGetValue("@UsingAttributes", out var usingAttributesBodyElement) != true ||
                usingAttributesBodyElement.ValueKind != JsonValueKind.Array ||
                usingAttributesBodyElement.GetArrayLength() != 2 ||
                usingAttributesBodyElement[0].ValueKind != JsonValueKind.Object)
            {
                return base.RewriteObject(jsonProperties, rootRewriter);
            }

            var template = usingAttributesBodyElement[1];
            
            var attributes = usingAttributesBodyElement[0]
                .EnumerateObject()
                .ToDictionary(property => property.Name, property => property.Value);
            
            var augmentedRootRewriter = new ChainRewriter
            (
                new GetAttributeValueRewriter<JsonElement>(attributes),
                rootRewriter
            );
            
            return augmentedRootRewriter.Rewrite(template);
        }
    }
}