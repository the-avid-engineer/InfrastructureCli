using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;

namespace InfrastructureCli.Rewriters;

internal sealed class RootRewriter : RewriterBase, IRootRewriter
{
    private readonly IReadOnlyList<IRewriter> _topDownRewriters;
    private readonly IReadOnlyList<IRewriter> _bottomUpRewriters;

    public string CurrentPath { get; }
    
    private RootRewriter(IEnumerable<IRewriter> topDownRewriters, IEnumerable<IRewriter> bottomUpRewriters, string currentPath)
    {
        _topDownRewriters = topDownRewriters.ToImmutableArray();
        _bottomUpRewriters = bottomUpRewriters.ToImmutableArray();
        
        CurrentPath = currentPath;
    }

    private JsonElement Rewrite(JsonElement jsonElement, IRewriter rewriter)
    {
        return rewriter.Rewrite(jsonElement, this);
    }

    public JsonElement Rewrite(JsonElement jsonElement)
    {
        // Rewrite Top-Down
        jsonElement = _topDownRewriters
            .Aggregate(jsonElement, Rewrite);
        
        // Traverse
        jsonElement = jsonElement.ValueKind switch
        {
            JsonValueKind.Object => RewriteObject(jsonElement),
            JsonValueKind.Array => RewriteArray(jsonElement),
            _ => jsonElement
        };
        
        // Rewrite Bottom-Up
        return _bottomUpRewriters
            .Aggregate(jsonElement, Rewrite);
    }
        
    private JsonElement RewriteObject(JsonElement jsonObject)
    {
        return BuildJsonElement(jsonWriter =>
        {
            jsonWriter.WriteStartObject();

            foreach (var jsonProperty in jsonObject.EnumerateObject())
            {
                jsonWriter.WritePropertyName(jsonProperty.Name);
                    
                Rewrite(jsonProperty.Value).WriteTo(jsonWriter);
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
                Rewrite(jsonElement).WriteTo(jsonWriter);
            }

            jsonWriter.WriteEndArray();
        });
    }

    public IRootRewriter WithCurrentPath(string currentPath)
    {
        return new RootRewriter
        (
            _topDownRewriters,
            _bottomUpRewriters,
            currentPath
        );
    }

    public IRootRewriter PrependToBottomUp(IRewriter rewriter)
    {
        return new RootRewriter
        (
            _topDownRewriters,
            _bottomUpRewriters.Prepend(rewriter),
            CurrentPath
        );
    }

    public IRootRewriter PrependToTopDown(IRewriter rewriter)
    {
        return new RootRewriter
        (
            _topDownRewriters.Prepend(rewriter),
            _bottomUpRewriters,
            CurrentPath
        );
    }
    
    internal static IRootRewriter Create
    (
        Dictionary<string, JsonElement> globalAttributes,
        Dictionary<string, Dictionary<string, JsonElement>> allGlobalRegionAttributes,
        Dictionary<string, JsonElement> attributes,
        Dictionary<string, Dictionary<string, JsonElement>> allRegionAttributes,
        string currentPath,
        string region
    )
    {
        var bottomUpRewriters = new List<IRewriter>();
        
        // Non-Global, Region
        if (allRegionAttributes.TryGetValue(region, out var regionAttributes))
        {
            bottomUpRewriters.Add(new GetAttributeValueRewriter<JsonElement>(regionAttributes));
            bottomUpRewriters.Add(new AttributeValueDefinedRewriter(regionAttributes.Keys));
        }
        
        // Non-Global, Non-Region
        bottomUpRewriters.Add(new GetAttributeValueRewriter<JsonElement>(attributes));
        bottomUpRewriters.Add(new AttributeValueDefinedRewriter(attributes.Keys));
        
        // Global, Region
        if (allGlobalRegionAttributes.TryGetValue(region, out var globalRegionAttributes))
        {
            bottomUpRewriters.Add(new GetAttributeValueRewriter<JsonElement>(globalRegionAttributes));
            bottomUpRewriters.Add(new AttributeValueDefinedRewriter(globalRegionAttributes.Keys));
        }
        
        // Global, Non-Region
        bottomUpRewriters.Add(new GetAttributeValueRewriter<JsonElement>(globalAttributes));
        bottomUpRewriters.Add(new AttributeValueDefinedRewriter(globalAttributes.Keys));
        
        bottomUpRewriters.AddRange
        (
            new IRewriter[]
            {
                new MapElementsRewriter(),
                new MapNamedElementsRewriter(),
                new MapPropertiesRewriter(),
                new MapNamedPropertiesRewriter(),
                new UsingAttributesRewriter(),
                new GetPropertyValueRewriter(),
                new SpreadElementsRewriter(),
                new SpreadPropertiesRewriter(),
                new SerializeRewriter(),
                new IntProductionRewriter(),
                new IncludeFileFromPathRewriter(),
                new IncludeFileRewriter(),
            }
        );

        var topDownRewriters = new List<IRewriter>
        {
            new IncludeRawFileRewriter(),
            new UsingAttributeMacroRewriter(),
            new UsingMacrosRewriter()
        };

        return new RootRewriter
        (
            topDownRewriters.ToArray(),
            bottomUpRewriters.ToArray(),
            currentPath
        );
    }
}