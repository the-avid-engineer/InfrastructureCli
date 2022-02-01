using System.Linq;
using System.Text.Json;

namespace InfrastructureCli.Rewriters
{
    internal sealed class GetPropertyValueRewriter : RewriterBase
    {
        public GetPropertyValueRewriter(Utf8JsonWriter jsonWriter) : base(jsonWriter)
        {
        }

        protected override void RewriteObject(JsonProperty[] jsonProperties)
        {
            if (IsGetPropertyValue(jsonProperties) && RewriteGetPropertyValue(jsonProperties))
            {
                return;
            }

            base.RewriteObject(jsonProperties);
        }

        private static bool IsGetPropertyValue(JsonProperty[] jsonProperties)
        {
            return jsonProperties.Length == 1 &&
                   jsonProperties[0].Name == "@GetPropertyValue" &&
                   jsonProperties[0].Value.ValueKind == JsonValueKind.Array &&
                   jsonProperties[0].Value.GetArrayLength() == 2 &&
                   jsonProperties[0].Value[0].ValueKind == JsonValueKind.Object &&
                   jsonProperties[0].Value[1].ValueKind == JsonValueKind.String;
        }

        private bool RewriteGetPropertyValue(JsonProperty[] jsonProperties)
        {
            var properties = jsonProperties[0].Value[0].EnumerateObject().ToDictionary(property => property.Name, property => property.Value);
            var propertyName = jsonProperties[0].Value[1].GetString()!;

            if (properties.TryGetValue(propertyName, out var propertyValue) == false)
            {
                return false;
            }

            propertyValue.WriteTo(JsonWriter);
            
            return true;
        }
    }
}
