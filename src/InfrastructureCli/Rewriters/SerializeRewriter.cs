using System.Text.Json;
using InfrastructureCli.Services;

namespace InfrastructureCli.Rewriters;

internal sealed class SerializeRewriter : RewriterBase, IRewriter
{
    public JsonElement Rewrite(JsonElement jsonElement, IRootRewriter rootRewriter)
    {
        if (TryGetArguments(jsonElement, "Serialize", out var argumentsElement) != true)
        {
            return jsonElement;
        }

        var serialized = JsonService.Serialize(argumentsElement);
            
        return BuildJsonElement(jsonWriter =>
        {
            jsonWriter.WriteStringValue(serialized);
        });
    }
}