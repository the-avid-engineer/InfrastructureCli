using System.Collections.Generic;
using System.Text.Json;
using InfrastructureCli.Services;

namespace InfrastructureCli.Rewriters
{
    internal sealed class GetAttributeValueRewriter<TAttributeValue> : RewriterBase
    {
        private readonly Dictionary<string, TAttributeValue> _attributes;

        public GetAttributeValueRewriter(Dictionary<string, TAttributeValue> attributes)
        {
            _attributes = attributes;
        }

        public override JsonElement Rewrite(JsonElement jsonElement, IRewriter rootRewriter)
        {
            var templateJson = jsonElement.GetRawText();
        
            foreach (var (key, value) in _attributes)
            {
                templateJson = templateJson.Replace($"@{{{key}}}", value?.ToString()?.Replace("\"", "\\\""));
            }

            var implicitRewritten = JsonService.Deserialize<JsonElement>(templateJson);
            
            return base.Rewrite(implicitRewritten, rootRewriter);
        }
        
        protected override JsonElement RewriteObject(IReadOnlyDictionary<string, JsonElement> jsonProperties, IRewriter rootRewriter)
        {
            if (TryGetArgumentsElement(jsonProperties, "GetAttributeValue", out var getAttributeValueArgumentsElement) != true ||
                getAttributeValueArgumentsElement.ValueKind != JsonValueKind.String)
            {
                return base.RewriteObject(jsonProperties, rootRewriter);
            }

            var attributeName = getAttributeValueArgumentsElement.GetString()!;

            if (_attributes.TryGetValue(attributeName, out var attributeValue) == false)
            {
                return base.RewriteObject(jsonProperties, rootRewriter);
            }

            return Rewrite(jsonWriter =>
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
