using System.Linq;
using System.Text.Json;

namespace InfrastructureCli.Rewriters;

internal class UsingMacrosRewriter : RewriterBase, IRewriter
{
    public JsonElement Rewrite(JsonElement jsonElement, IRootRewriter rootRewriter)
    {
        if (TryGetArguments(jsonElement, "UsingMacros", out var argumentsElement) != true ||
            argumentsElement.ValueKind != JsonValueKind.Array ||
            argumentsElement.GetArrayLength() != 2 ||
            TryGetProperties(argumentsElement[0], out var childJsonProperties) != true)
        {
            return jsonElement;
        }
            
        var macros = childJsonProperties
            .ToDictionary(property => property.Name, property => rootRewriter.Rewrite(property.Value));

        var templateJsonElement = argumentsElement[1];

        return rootRewriter
            .PrependToTopDown(new GetMacroRewriter(macros))
            .Rewrite(templateJsonElement);
    }
}