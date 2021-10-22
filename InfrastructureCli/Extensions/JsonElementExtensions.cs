using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using InfrastructureCli.Rewriters;

namespace InfrastructureCli.Extensions
{
    public static class JsonElementExtensions
    {
        public static JsonElement RewriteGetPropertyValues(this JsonElement jsonElement)
        {
            using var memoryStream = new MemoryStream();
            using var jsonWriter = new Utf8JsonWriter(memoryStream);

            var rewriter = new GetPropertyValueRewriter(jsonWriter);

            rewriter.Rewrite(jsonElement);

            jsonWriter.Flush();

            return JsonSerializer.Deserialize<JsonElement>(memoryStream.ToArray());
        }

        public static JsonElement RewriteGetAttributeValues<TAttributeValue>(this JsonElement jsonElement, Dictionary<string, TAttributeValue> attributes)
        {
            var templateJson = jsonElement.GetRawText();

            foreach (var (key, value) in attributes)
            {
                templateJson = templateJson.Replace("@{" + key + "}", value?.ToString()?.Replace("\"", "\\\""));
            };

            jsonElement = JsonSerializer.Deserialize<JsonElement>(templateJson);

            using var memoryStream = new MemoryStream();
            using var jsonWriter = new Utf8JsonWriter(memoryStream);

            var rewriter = new GetAttributeValueRewriter<TAttributeValue>(jsonWriter, attributes);

            rewriter.Rewrite(jsonElement);

            jsonWriter.Flush();

            return JsonSerializer.Deserialize<JsonElement>(memoryStream.ToArray());
        }

        public static JsonElement RewriteMaps(this JsonElement jsonElement)
        {
            using var memoryStream = new MemoryStream();
            using var jsonWriter = new Utf8JsonWriter(memoryStream);

            var rewriter = new MapRewriter(jsonWriter);

            rewriter.Rewrite(jsonElement);

            jsonWriter.Flush();

            return JsonSerializer.Deserialize<JsonElement>(memoryStream.ToArray());
        }

        public static JsonElement RewriteSpreads(this JsonElement jsonElement)
        {
            using var memoryStream = new MemoryStream();
            using var jsonWriter = new Utf8JsonWriter(memoryStream);

            var rewriter = new SpreadRewriter(jsonWriter);

            rewriter.Rewrite(jsonElement);

            jsonWriter.Flush();

            return JsonSerializer.Deserialize<JsonElement>(memoryStream.ToArray());
        }
    }
}
