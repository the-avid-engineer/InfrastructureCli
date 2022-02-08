using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace InfrastructureCli.Rewriters
{
    internal abstract class RewriterBase : IRewriter
    {
        public virtual JsonElement Rewrite(JsonElement jsonElement, IRewriter rootRewriter)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.Object => RewriteObject(jsonElement, rootRewriter),
                JsonValueKind.Array => RewriteArray(jsonElement, rootRewriter),
                _ => jsonElement
            };
        }

        private JsonElement RewriteObject(JsonElement jsonObject, IRewriter rootRewriter)
        {
            var newJsonProperties = new Dictionary<string, JsonElement>();
            
            foreach (var jsonProperty in jsonObject.EnumerateObject())
            {
                var newValue = Rewrite(jsonProperty.Value, rootRewriter);

                newJsonProperties.Add(jsonProperty.Name, newValue);
            }

            return RewriteObject(newJsonProperties, rootRewriter);
        }

        private JsonElement RewriteArray(JsonElement jsonArray, IRewriter rootRewriter)
        {
            return Rewrite(jsonWriter =>
            {
                jsonWriter.WriteStartArray();

                foreach (var jsonElement in jsonArray.EnumerateArray())
                {
                    Rewrite(jsonElement, rootRewriter).WriteTo(jsonWriter);
                }

                jsonWriter.WriteEndArray();
            });
        }

        protected JsonElement Rewrite(Action<Utf8JsonWriter> processor)
        {
            using var memoryStream = new MemoryStream();
            using var jsonWriter = new Utf8JsonWriter(memoryStream);
            
            processor.Invoke(jsonWriter);
            
            jsonWriter.Flush();

            return JsonSerializer.Deserialize<JsonElement>(memoryStream.ToArray());
        }

        protected virtual JsonElement RewriteObject(IReadOnlyDictionary<string, JsonElement> jsonProperties, IRewriter rootRewriter)
        {
            return Rewrite(jsonWriter =>
            {
                jsonWriter.WriteStartObject();

                foreach (var (name, value) in jsonProperties)
                {
                    jsonWriter.WritePropertyName(name);

                    value.WriteTo(jsonWriter);
                }

                jsonWriter.WriteEndObject();
            });
        }
    }
}
