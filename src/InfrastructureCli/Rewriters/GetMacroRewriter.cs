using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;

namespace InfrastructureCli.Rewriters;

internal sealed class GetMacroRewriter : RewriterBase, IRewriter
{
    private readonly Dictionary<string, JsonElement> _macros;

    public GetMacroRewriter(Dictionary<string, JsonElement> macros)
    {
        _macros = macros;
    }

    public JsonElement Rewrite(JsonElement jsonElement, IRootRewriter rootRewriter)
    {
        Debug.WriteLine(jsonElement.ToString());
        
        if (TryGetArguments(jsonElement, "GetMacro", out var argumentsElement) != true ||
            argumentsElement.ValueKind != JsonValueKind.String)
        {
            return jsonElement;
        }

        return _macros.TryGetValue(argumentsElement.GetString()!, out var macro) == false
            ? jsonElement
            : rootRewriter.Rewrite(macro);
    }
}