using System.Collections.Generic;
using System.Text.Json;
using InfrastructureCli.Services;

namespace InfrastructureCli.Rewriters
{
    internal sealed class GetAttributeValueRewriter<TAttributeValue> : RewriterBase, IRewriter
    {
        private readonly Dictionary<string, TAttributeValue> _attributes;

        public GetAttributeValueRewriter(Dictionary<string, TAttributeValue> attributes)
        {
            _attributes = attributes;
        }

        public JsonElement Rewrite(JsonElement jsonElement, IRewriter rootRewriter)
        {
            return RewriteExplicit(RewriteImplicit(jsonElement));
        }

        private JsonElement RewriteImplicit(JsonElement jsonElement)
        {
            var templateJson = jsonElement.GetRawText();
        
            foreach (var (key, value) in _attributes)
            {
                templateJson = templateJson.Replace($"@{{{key}}}", value?.ToString()?.Replace("\"", "\\\""));
            }

            return JsonService.Deserialize<JsonElement>(templateJson);
        }
        
        private JsonElement RewriteExplicit(JsonElement jsonElement)
        {
            if (TryGetArguments(jsonElement, "GetAttributeValue", out var argumentsElement) != true ||
                argumentsElement.ValueKind != JsonValueKind.String)
            {
                return jsonElement;
            }

            var attributeName = argumentsElement.GetString()!;

            if (_attributes.TryGetValue(attributeName, out var attributeValue) == false)
            {
                return jsonElement;
            }

            return BuildJsonElement(jsonWriter =>
            {
                if (object.Equals(attributeValue, default))
                {
                    jsonWriter.WriteNullValue();
                }
                else
                {
                    var attributeValueElement = JsonService.Convert<TAttributeValue, JsonElement>(attributeValue);

                    attributeValueElement.WriteTo(jsonWriter);
                }
            });
        }
    }
}
