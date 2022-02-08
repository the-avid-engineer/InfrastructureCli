using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace InfrastructureCli.Rewriters
{
    internal sealed class GetPropertyValueRewriter : RewriterBase
    {
        protected override JsonElement RewriteObject(IReadOnlyDictionary<string, JsonElement> jsonProperties, IRewriter rootRewriter)
        {
            if (jsonProperties.Count != 1 ||
                jsonProperties.TryGetValue("@GetPropertyValue", out var getPropertyValueElement) != true ||
                getPropertyValueElement.ValueKind != JsonValueKind.Array ||
                getPropertyValueElement.GetArrayLength() != 2 ||
                getPropertyValueElement[0].ValueKind != JsonValueKind.Object ||
                getPropertyValueElement[1].ValueKind != JsonValueKind.String)
            {
                return base.RewriteObject(jsonProperties, rootRewriter);
            }
            
            var properties = getPropertyValueElement[0]
                .EnumerateObject()
                .ToDictionary(property => property.Name, property => property.Value);

            var propertyName = getPropertyValueElement[1].GetString()!;
            
            return properties.TryGetValue(propertyName, out var propertyValueElement)
                ? propertyValueElement
                : base.RewriteObject(jsonProperties, rootRewriter);
        }
    }
}
