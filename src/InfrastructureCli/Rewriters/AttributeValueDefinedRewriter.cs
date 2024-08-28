using System.Text.Json;

namespace InfrastructureCli.Rewriters;

internal sealed class AttributeValueDefinedRewriter : RewriterBase, IRewriter
{
    private readonly ICollection<string> _attributeNames;

    public AttributeValueDefinedRewriter(ICollection<string> attributeNames)
    {
        _attributeNames = attributeNames;
    }

    private static readonly JsonElement TrueElement = BuildJsonElement(jsonWriter =>
    {
        jsonWriter.WriteBooleanValue(true);
    });

    private static readonly JsonElement FalseElement = BuildJsonElement(jsonWriter =>
    {
        jsonWriter.WriteBooleanValue(false);
    });

    public JsonElement Rewrite(JsonElement jsonElement, IRootRewriter rootRewriter)
    {
        if (TryGetArguments(jsonElement, "AttributeValueDefined", out var argumentsElement) != true ||
            argumentsElement.ValueKind != JsonValueKind.String)
        {
            return jsonElement;
        }

        var attributeName = argumentsElement.GetString()!;

        return _attributeNames.Contains(attributeName) ? TrueElement : FalseElement;
    }
}