using System.Collections.Generic;
using System.Text.Json;

namespace InfrastructureCli.Rewriters
{
    internal sealed class GetAttributeValueRewriter<TAttributeValue> : RewriterBase
    {
        private readonly Dictionary<string, TAttributeValue> _attributes;

        public GetAttributeValueRewriter(Utf8JsonWriter jsonWriter, Dictionary<string, TAttributeValue> attributes) : base(jsonWriter)
        {
            _attributes = attributes;
        }

        protected override void RewriteObject(JsonProperty[] jsonProperties)
        {
            if (IsGetAttributeValue(jsonProperties) && RewriteGetAttributeValue(jsonProperties))
            {
                return;
            }

            base.RewriteObject(jsonProperties);
        }

        private bool IsGetAttributeValue(JsonProperty[] jsonProperties)
        {
            return jsonProperties.Length == 1 &&
                   jsonProperties[0].Name == "@GetAttributeValue" &&
                   jsonProperties[0].Value.ValueKind == JsonValueKind.String;
        }

        private bool RewriteGetAttributeValue(JsonProperty[] jsonProperties)
        {
            var attributeName = jsonProperties[0].Value.GetString()!;

            if (_attributes.TryGetValue(attributeName, out var attributeValue) == false)
            {
                return false;
            }

            if (object.Equals(attributeValue, default))
            {
                JsonWriter.WriteNullValue();
            }
            else
            {
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(attributeValue));

                jsonElement.WriteTo(JsonWriter);
            }

            return true;
        }
    }
}
