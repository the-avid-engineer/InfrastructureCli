using System.Collections.Generic;
using System.Text.Json;
using InfrastructureCli.Services;
using InfrastructureCli.Extensions;

namespace InfrastructureCli.Rewriters
{
    internal sealed class SerializeRewriter : RewriterBase
    {
        protected override JsonElement RewriteObject(IReadOnlyDictionary<string, JsonElement> jsonProperties, IRewriter rootRewriter)
        {
            if (jsonProperties.Count != 1 ||
                jsonProperties.TryGetValue("@Serialize", out var template) != true)
            {
                return base.RewriteObject(jsonProperties, rootRewriter);
            }
            
            template = rootRewriter.Rewrite(template);
            
            var serialized = JsonService.Serialize(template);

            return Rewrite(jsonWriter =>
            {
                jsonWriter.WriteStringValue(serialized);
            });
        }
    }
}
