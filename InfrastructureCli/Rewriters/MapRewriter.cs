using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using InfrastructureCli.Extensions;

namespace InfrastructureCli.Rewriters
{
    internal sealed class MapRewriter : RewriterBase
    {
        public MapRewriter(Utf8JsonWriter jsonWriter) : base(jsonWriter)
        {
        }

        protected override void RewriteObject(JsonProperty[] jsonProperties)
        {
            if (IsObjectMap(jsonProperties))
            {
                RewriteObjectMap(jsonProperties);
            }
            else if (IsArrayMap(jsonProperties))
            {
                RewriteArrayMap(jsonProperties);
            }
            else
            {
                base.RewriteObject(jsonProperties);
            }
        }

        private static bool IsObjectMap(JsonProperty[] jsonProperties)
        {
            return jsonProperties.Length == 1 &&
                   jsonProperties[0].Name == "@Map" &&
                   jsonProperties[0].Value.ValueKind == JsonValueKind.Array &&
                   jsonProperties[0].Value.GetArrayLength() == 2 &&
                   jsonProperties[0].Value[0].ValueKind == JsonValueKind.Object;
        }

        private void RewriteObjectMap(JsonProperty[] jsonProperties)
        {
            var template = jsonProperties[0].Value[1].RewriteMaps();

            var jsonElements = jsonProperties[0].Value[0]
                .EnumerateObject()
                .Select((property) =>
                {
                    var attributes = new Dictionary<string, dynamic>
                    {
                        ["Key"] = property.Name,
                        ["Value"] = property.Value,
                    };

                    return template.RewriteGetAttributeValues(attributes);
                });

            _jsonWriter.WriteStartArray();

            foreach (var jsonElement in jsonElements)
            {
                jsonElement.WriteTo(_jsonWriter);
            }

            _jsonWriter.WriteEndArray();
        }

        private static bool IsArrayMap(JsonProperty[] jsonProperties)
        {
            return jsonProperties.Length == 1 &&
                   jsonProperties[0].Name == "@Map" &&
                   jsonProperties[0].Value.ValueKind == JsonValueKind.Array &&
                   jsonProperties[0].Value.GetArrayLength() == 2 &&
                   jsonProperties[0].Value[0].ValueKind == JsonValueKind.Array;
        }

        private void RewriteArrayMap(JsonProperty[] jsonProperties)
        {
            var template = jsonProperties[0].Value[1].RewriteMaps();

            var jsonElements = jsonProperties[0].Value[0]
                .EnumerateArray()
                .Select((value, index) =>
                {
                    var attributeService = new Dictionary<string, dynamic>
                    {
                        ["Index"] = index,
                        ["Value"] = value,
                    };

                    return template.RewriteGetAttributeValues(attributeService);
                });

            _jsonWriter.WriteStartArray();

            foreach (var jsonElement in jsonElements)
            {
                jsonElement.WriteTo(_jsonWriter);
            }

            _jsonWriter.WriteEndArray();
        }
    }
}
