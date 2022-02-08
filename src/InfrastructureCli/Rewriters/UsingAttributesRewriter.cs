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
            if (TryGetArgumentsElement(jsonProperties, "UsingAttributes", out var usingAttributesArgumentsElement) != true ||
                usingAttributesArgumentsElement.ValueKind != JsonValueKind.Array ||
                usingAttributesArgumentsElement.GetArrayLength() != 2 ||
                usingAttributesArgumentsElement[0].ValueKind != JsonValueKind.Object)
            {
                return base.RewriteObject(jsonProperties, rootRewriter);
            }

            var template = usingAttributesArgumentsElement[1];
            
            var attributes = usingAttributesArgumentsElement[0]
                .EnumerateObject()
                .ToDictionary(property => property.Name, property => property.Value);

            if (IsFunctionObject(attributes))
            {
                return base.RewriteObject(jsonProperties, rootRewriter);
            }
            
            var augmentedRootRewriter = new ChainRewriter
            (
                new GetAttributeValueRewriter<JsonElement>(attributes),
                rootRewriter
            );
            
            return augmentedRootRewriter.Rewrite(template);
        }
    }
}