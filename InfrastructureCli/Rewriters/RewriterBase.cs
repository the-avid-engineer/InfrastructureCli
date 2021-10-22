using System.Linq;
using System.Text.Json;

namespace InfrastructureCli.Rewriters
{
    public abstract class RewriterBase
    {
        protected readonly Utf8JsonWriter _jsonWriter;

        public RewriterBase(Utf8JsonWriter jsonWriter)
        {
            _jsonWriter = jsonWriter;
        }

        public void Rewrite(JsonElement jsonElement)
        {
            switch (jsonElement.ValueKind)
            {
                case JsonValueKind.Array:
                    RewriteArray(jsonElement.EnumerateArray().ToArray());
                    break;

                case JsonValueKind.Object:
                    RewriteObject(jsonElement.EnumerateObject().ToArray());
                    break;

                default:
                    RewriteScalar(jsonElement);
                    break;
            }
        }

        protected virtual void RewriteArray(JsonElement[] jsonElements)
        {
            _jsonWriter.WriteStartArray();

            foreach (var childElement in jsonElements)
            {
                Rewrite(childElement);
            }

            _jsonWriter.WriteEndArray();
        }

        protected virtual void RewriteObject(JsonProperty[] jsonProperties)
        {
            _jsonWriter.WriteStartObject();

            foreach (var jsonProperty in jsonProperties)
            {
                _jsonWriter.WritePropertyName(jsonProperty.Name);

                Rewrite(jsonProperty.Value);
            }

            _jsonWriter.WriteEndObject();
        }

        protected virtual void RewriteScalar(JsonElement jsonElement)
        {
            jsonElement.WriteTo(_jsonWriter);
        }
    }
}
