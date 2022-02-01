using System.Linq;
using System.Text.Json;

namespace InfrastructureCli.Rewriters
{
    public abstract class RewriterBase
    {
        protected readonly Utf8JsonWriter JsonWriter;

        protected RewriterBase(Utf8JsonWriter jsonWriter)
        {
            JsonWriter = jsonWriter;
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
            JsonWriter.WriteStartArray();

            foreach (var childElement in jsonElements)
            {
                Rewrite(childElement);
            }

            JsonWriter.WriteEndArray();
        }

        protected virtual void RewriteObject(JsonProperty[] jsonProperties)
        {
            JsonWriter.WriteStartObject();

            foreach (var jsonProperty in jsonProperties)
            {
                JsonWriter.WritePropertyName(jsonProperty.Name);

                Rewrite(jsonProperty.Value);
            }

            JsonWriter.WriteEndObject();
        }

        protected virtual void RewriteScalar(JsonElement jsonElement)
        {
            jsonElement.WriteTo(JsonWriter);
        }
    }
}
