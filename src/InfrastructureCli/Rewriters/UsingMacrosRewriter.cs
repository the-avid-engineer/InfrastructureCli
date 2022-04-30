using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using InfrastructureCli.Extensions;

namespace InfrastructureCli.Rewriters;

internal class UsingMacrosRewriter : RewriterBase, IRewriter
{
    public JsonElement Rewrite(JsonElement jsonElement, IRewriter rootRewriter)
    {
        if (TryGetArguments(jsonElement, "UsingMacros", out var argumentsElement) != true ||
            argumentsElement.ValueKind != JsonValueKind.Array ||
            argumentsElement.GetArrayLength() != 2 ||
            TryGetProperties(argumentsElement[0], out var childJsonProperties) != true)
        {
            return jsonElement;
        }
            
        var macros = childJsonProperties
            .ToDictionary(property => property.Name, property => property.Value);

        var templateJsonElement = argumentsElement[1];

        var rewriter = new TopDownChainRewriter
        (
            new GetMacroRewriter(macros)
        );
            
        return rewriter.Rewrite(templateJsonElement);
    }
}