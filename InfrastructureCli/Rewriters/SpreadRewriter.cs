using System.Linq;
using System.Text.Json;
using InfrastructureCli.Extensions;

namespace InfrastructureCli.Rewriters
{
    internal sealed class SpreadRewriter : RewriterBase
    {
        public SpreadRewriter(Utf8JsonWriter jsonWriter) : base(jsonWriter)
        {
        }

        protected override void RewriteObject(JsonProperty[] jsonProperties)
        {
            if (IsObjectSpread(jsonProperties))
            {
                RewriteObjectSpread(jsonProperties);
            }
            else if (IsArraySpread(jsonProperties))
            {
                RewriteArraySpread(jsonProperties);
            }
            else
            {
                base.RewriteObject(jsonProperties);
            }
        }

        private bool IsObjectSpread(JsonProperty[] jsonProperties)
        {
            return jsonProperties.Length == 1 &&
                   jsonProperties[0].Name == "@Spread" &&
                   jsonProperties[0].Value.ValueKind == JsonValueKind.Array &&
                   jsonProperties[0].Value.EnumerateArray().All(value => value.ValueKind == JsonValueKind.Object);
        }

        private void RewriteObjectSpread(JsonProperty[] jsonProperties)
        {
            var allJsonProperties = jsonProperties[0].Value.EnumerateArray()
                .SelectMany(jsonElement => jsonElement.RewriteSpreads().EnumerateObject())
                .ToArray();

            _jsonWriter.WriteStartObject();

            foreach (var jsonProperty in allJsonProperties)
            {
                jsonProperty.WriteTo(_jsonWriter);
            }

            _jsonWriter.WriteEndObject();
        }

        private bool IsArraySpread(JsonProperty[] jsonProperties)
        {
            return jsonProperties.Length == 1 &&
                   jsonProperties[0].Name == "@Spread" &&
                   jsonProperties[0].Value.ValueKind == JsonValueKind.Array &&
                   jsonProperties[0].Value.EnumerateArray().All(value => value.ValueKind == JsonValueKind.Array);
        }

        private void RewriteArraySpread(JsonProperty[] jsonProperties)
        {
            var allJsonElements = jsonProperties[0].Value.EnumerateArray()
                .SelectMany(jsonElement => jsonElement.RewriteSpreads().EnumerateArray())
                .ToArray();

            _jsonWriter.WriteStartArray();

            foreach (var jsonElement in allJsonElements)
            {
                jsonElement.WriteTo(_jsonWriter);
            }

            _jsonWriter.WriteEndArray();
        }
    }
}
