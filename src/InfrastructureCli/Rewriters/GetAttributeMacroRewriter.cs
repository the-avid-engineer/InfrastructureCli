using System.Linq;
using System.Text.Json;

namespace InfrastructureCli.Rewriters;

internal class GetAttributeMacroRewriter : RewriterBase, IRewriter
{
    private readonly string _name;
    private readonly string[] _attributeNames;
    private readonly JsonElement _template;

    public GetAttributeMacroRewriter(string name, string[] attributeNames, JsonElement template)
    {
        _name = name;
        _attributeNames = attributeNames;
        _template = template;
    }

    public JsonElement Rewrite(JsonElement jsonElement, IRootRewriter rootRewriter)
    {
        if (TryGetArguments(jsonElement, "GetAttributeMacro", out var argumentsElement) != true ||
            TryGetElements(argumentsElement, out var argumentElements) != true ||
            argumentElements.Length != _attributeNames.Length + 1 ||
            TryGetString(argumentElements[0], out var attributeMacroName) != true ||
            attributeMacroName != _name
        )
        {
            return jsonElement;
        }

        var attributesDictionary = _attributeNames
            .Zip(argumentElements.Skip(1))
            .ToDictionary(pair => pair.First, pair => pair.Second);

        return BuildJsonElement(jsonWriter =>
        {
            jsonWriter.WriteStartObject();

            jsonWriter.WritePropertyName("@Fn::UsingAttributes");

            jsonWriter.WriteStartArray();

            jsonWriter.WriteStartObject();

            foreach (var attribute in attributesDictionary)
            {
                jsonWriter.WritePropertyName(attribute.Key);
                attribute.Value.WriteTo(jsonWriter);
            }

            jsonWriter.WriteEndObject();

            _template.WriteTo(jsonWriter);

            jsonWriter.WriteEndArray();

            jsonWriter.WriteEndObject();
        });
    }
}