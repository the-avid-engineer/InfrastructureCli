using System.Linq;
using System.Text.Json;

namespace InfrastructureCli.Rewriters;

internal sealed class ChainRewriter : RewriterBase, IRewriter
{
    public static readonly IRewriter Base = new ChainRewriter
    (
        new MapElementsRewriter(),
        new MapPropertiesRewriter(),
        new UsingAttributesRewriter(),
        new GetPropertyValueRewriter(),
        new SpreadElementsRewriter(),
        new SpreadPropertiesRewriter(),
        new SerializeRewriter(),
        new IntProductionRewriter()
    );

    public static IRewriter ForCurrentPath(string currentPath)
    {
        return new ChainRewriter
        (
            new IncludeFileRewriter(currentPath),
            new IncludeRawFileRewriter(currentPath)
        );
    }

    private readonly IRewriter[] _rewriters;

    public ChainRewriter(params IRewriter[] rewriters)
    {
        _rewriters = rewriters;
    }

    public JsonElement Rewrite(JsonElement jsonElement, IRewriter rootRewriter)
    {
        jsonElement = jsonElement.ValueKind switch
        {
            JsonValueKind.Object => RewriteObject(jsonElement),
            JsonValueKind.Array => RewriteArray(jsonElement),
            _ => jsonElement
        };

        return _rewriters
            .Aggregate(jsonElement, (currentJsonElement, rewriter) => rewriter.Rewrite(currentJsonElement, rootRewriter));
    }
        
    private JsonElement RewriteObject(JsonElement jsonObject)
    {
        return BuildJsonElement(jsonWriter =>
        {
            jsonWriter.WriteStartObject();

            foreach (var jsonProperty in jsonObject.EnumerateObject())
            {
                jsonWriter.WritePropertyName(jsonProperty.Name);
                    
                Rewrite(jsonProperty.Value, this).WriteTo(jsonWriter);
            }

            jsonWriter.WriteEndObject();
        });
    }
        
    private JsonElement RewriteArray(JsonElement jsonArray)
    {
        return BuildJsonElement(jsonWriter =>
        {
            jsonWriter.WriteStartArray();

            foreach (var jsonElement in jsonArray.EnumerateArray())
            {
                Rewrite(jsonElement, this).WriteTo(jsonWriter);
            }

            jsonWriter.WriteEndArray();
        });
    }
}