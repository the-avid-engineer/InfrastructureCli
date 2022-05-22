using System.Text.Json;

namespace InfrastructureCli.Rewriters;

internal class UsingAttributeMacroRewriter : RewriterBase, IRewriter
{
    public JsonElement Rewrite(JsonElement jsonElement, IRootRewriter rootRewriter)
    {
        if (TryGetArguments(jsonElement, "UsingAttributeMacro", out var argumentsElement) != true ||
            argumentsElement.ValueKind != JsonValueKind.Array ||
            argumentsElement.GetArrayLength() != 4 ||
            TryGetString(argumentsElement[0], out var macroName) != true ||
            TryGetStrings(argumentsElement[1], out var attributeNames) != true)
        {
            return jsonElement;
        }

        var macroTemplate = argumentsElement[2];
        var templateJsonElement = argumentsElement[3];

        return rootRewriter
            .PrependToTopDown(new GetAttributeMacroRewriter(macroName, attributeNames, macroTemplate))
            .Rewrite(templateJsonElement);
    }
}
