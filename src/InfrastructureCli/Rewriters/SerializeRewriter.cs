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
            if (TryGetArgumentsElement(jsonProperties, "Serialize", out var serializeArgumentsElement) != true)
            {
                return base.RewriteObject(jsonProperties, rootRewriter);
            }
            
            serializeArgumentsElement = rootRewriter.Rewrite(serializeArgumentsElement);
            
            var serialized = JsonService.Serialize(serializeArgumentsElement);

            return Rewrite(jsonWriter =>
            {
                jsonWriter.WriteStringValue(serialized);
            });
        }
    }
}
