﻿using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace InfrastructureCli.Rewriters;

internal abstract class RewriterBase
{
    protected static bool IsNormalObject(JsonElement jsonElement)
    {
        return TryGetProperties(jsonElement, out _);
    }
        
    protected static bool TryGetProperties(JsonElement jsonElement, out JsonProperty[] jsonProperties)
    {
        jsonProperties = default!;
            
        if (jsonElement.ValueKind != JsonValueKind.Object)
        {
            return false;
        }
            
        jsonProperties = jsonElement
            .EnumerateObject()
            .ToArray();

        return jsonProperties.Length != 1 || !jsonProperties[0].Name.StartsWith("@Fn::");
    }
        
    protected static bool TryGetArguments(JsonElement jsonElement, string functionName, out JsonElement argumentsElement)
    {
        argumentsElement = default!;

        if (jsonElement.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        var jsonProperties = jsonElement.EnumerateObject().ToArray();

        if (jsonProperties.Length != 1 || jsonProperties[0].Name != $"@Fn::{functionName}")
        {
            return false;
        }

        argumentsElement = jsonProperties[0].Value;
        return true;
    }
        
    protected static JsonElement BuildJsonElement(Action<Utf8JsonWriter> processor)
    {
        using var memoryStream = new MemoryStream();
        using var jsonWriter = new Utf8JsonWriter(memoryStream);
            
        processor.Invoke(jsonWriter);
            
        jsonWriter.Flush();

        return JsonSerializer.Deserialize<JsonElement>(memoryStream.ToArray());
    }
}