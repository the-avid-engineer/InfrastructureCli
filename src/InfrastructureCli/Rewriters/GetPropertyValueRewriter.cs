using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using InfrastructureCli.Extensions;

namespace InfrastructureCli.Rewriters
{
    internal sealed class GetPropertyValueRewriter : RewriterBase
    {
        protected override JsonElement RewriteObject(IReadOnlyDictionary<string, JsonElement> jsonProperties, IRewriter rootRewriter)
        {
            if (TryGetArgumentsElement(jsonProperties, "GetPropertyValue", out var getPropertyValueArgumentsElement) != true ||
                getPropertyValueArgumentsElement.ValueKind != JsonValueKind.Array ||
                getPropertyValueArgumentsElement.GetArrayLength() != 2 ||
                getPropertyValueArgumentsElement[0].ValueKind != JsonValueKind.Object ||
                getPropertyValueArgumentsElement[1].ValueKind != JsonValueKind.String)
            {
                return base.RewriteObject(jsonProperties, rootRewriter);
            }
            
            var properties = getPropertyValueArgumentsElement[0]
                .EnumerateObject()
                .ToDictionary(property => property.Name, property => property.Value);

            if (IsFunctionObject(properties))
            {
                return base.RewriteObject(jsonProperties, rootRewriter);
            }
            
            var propertyName = getPropertyValueArgumentsElement[1].GetString()!;
            
            return properties.TryGetValue(propertyName, out var propertyValueElement)
                ? propertyValueElement
                : base.RewriteObject(jsonProperties, rootRewriter);
        }
    }
}
