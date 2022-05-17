using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;

namespace InfrastructureCli.Rewriters;

internal sealed class TopDownChainRewriter : RewriterBase, IRewriter
{
    public static readonly IRewriter Base = new TopDownChainRewriter
    (
        new UsingMacrosRewriter()
    );
    
    public static IRewriter ForCurrentPath(string currentPath)
    {
        return new TopDownChainRewriter
        (
            new IncludeRawFileRewriter(currentPath)
        );
    }


    private readonly IRewriter[] _rewriters;

    public TopDownChainRewriter(params IRewriter[] rewriters)
    {
        _rewriters = rewriters;
    }

    public JsonElement Rewrite(JsonElement jsonElement, IRewriter rootRewriter)
    {
        jsonElement = _rewriters
            .Aggregate(jsonElement,
                (currentJsonElement, rewriter) => rewriter.Rewrite(currentJsonElement, rootRewriter));

        return jsonElement.ValueKind switch
        {
            JsonValueKind.Object => RewriteObject(jsonElement, rootRewriter),
            JsonValueKind.Array => RewriteArray(jsonElement, rootRewriter),
            _ => jsonElement
        };
    }
        
    private JsonElement RewriteObject(JsonElement jsonObject, IRewriter rootRewriter)
    {
        return BuildJsonElement(jsonWriter =>
        {
            jsonWriter.WriteStartObject();

            foreach (var jsonProperty in jsonObject.EnumerateObject())
            {
                jsonWriter.WritePropertyName(jsonProperty.Name);
                    
                Rewrite(jsonProperty.Value, rootRewriter).WriteTo(jsonWriter);
            }

            jsonWriter.WriteEndObject();
        });
    }
        
    private JsonElement RewriteArray(JsonElement jsonArray, IRewriter rootRewriter)
    {
        return BuildJsonElement(jsonWriter =>
        {
            jsonWriter.WriteStartArray();

            foreach (var jsonElement in jsonArray.EnumerateArray())
            {
                Rewrite(jsonElement, rootRewriter).WriteTo(jsonWriter);
            }

            jsonWriter.WriteEndArray();
        });
    }
}