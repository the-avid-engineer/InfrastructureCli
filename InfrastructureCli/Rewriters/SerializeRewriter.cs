using System.Linq;
using System.Text.Json;
using InfrastructureCli.Extensions;
using InfrastructureCli.Services;

namespace InfrastructureCli.Rewriters
{
    internal sealed class SerializeRewriter : RewriterBase
    {
        public SerializeRewriter(Utf8JsonWriter jsonWriter) : base(jsonWriter)
        {
        }

        protected override void RewriteObject(JsonProperty[] jsonProperties)
        {
            if (IsSerialize(jsonProperties))
            {
                RewriteSerialize(jsonProperties);
            }
            else
            {
                base.RewriteObject(jsonProperties);
            }
        }

        private bool IsSerialize(JsonProperty[] jsonProperties)
        {
            return jsonProperties.Length == 1 &&
                   jsonProperties[0].Name == "@Serialize";
        }

        private void RewriteSerialize(JsonProperty[] jsonProperties)
        {
            var serialized = JsonService.Serialize(jsonProperties[0].Value);

            _jsonWriter.WriteStringValue(serialized);
        }
    }
}
