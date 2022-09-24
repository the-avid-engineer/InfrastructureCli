using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace InfrastructureCli.Services;

public static class JsonService
{
    private static readonly JsonSerializerOptions Indented = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,        
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    private static readonly JsonSerializerOptions Flat = new()
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public static bool Validate(JsonElement jsonElement, [NotNullWhen(false)] out string? hintPath)
    {
        return ValidateUnknown(jsonElement, "$", out hintPath);
    }
    
    private static bool ValidateUnknown(JsonElement jsonElement, string parentPath, [NotNullWhen(false)] out string? hintPath)
    {
        hintPath = null;
        
        return jsonElement.ValueKind switch
        {
            JsonValueKind.Object => ValidateObject(jsonElement, parentPath, out hintPath),
            JsonValueKind.Array => ValidateArray(jsonElement, parentPath, out hintPath),
            _ => true
        };
    }

    private static bool ValidateArray(JsonElement jsonArray, string parentPath, [NotNullWhen(false)] out string? hintPath)
    {
        hintPath = null;

        var jsonElements = jsonArray.EnumerateArray().ToArray();

        for (var i = 0; i < jsonElements.Length; i++)
        {
            var jsonElement = jsonElements[i];
            
            if (!ValidateUnknown(jsonElement, $"{parentPath}[{i}]", out hintPath))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ValidateObject(JsonElement jsonObject, string parentPath, [NotNullWhen(false)] out string? hintPath)
    {
        hintPath = null;
        
        var propertyNames = new HashSet<string>();
        
        foreach (var jsonProperty in jsonObject.EnumerateObject())
        {
            var thisPath = $"{parentPath}['{jsonProperty.Name}']";
            
            if (!propertyNames.Add(jsonProperty.Name))
            {
                hintPath = thisPath;
                return false;
            }

            if (!ValidateUnknown(jsonProperty.Value, thisPath, out hintPath))
            {
                return false;
            }
        }

        return true;
    }

    public static TOut Deserialize<TOut>(string json)
    {
        return JsonSerializer.Deserialize<TOut>(json, Indented)!;
    }

    public static async Task<TOut> DeserializeAsync<TOut>(Stream stream)
    {
        return (await JsonSerializer.DeserializeAsync<TOut>(stream, Indented))!;
    }

    public static async Task SerializeAsync<TIn>(TIn @in, Stream stream)
    {
        await JsonSerializer.SerializeAsync(stream, @in, Indented);
    }

    public static string Serialize<TIn>(TIn @in)
    {
        return JsonSerializer.Serialize(@in, Flat);
    }

    public static TOut Convert<TIn, TOut>(TIn @in)
    {
        var json = Serialize(@in);
            
        return JsonSerializer.Deserialize<TOut>(json, Flat)!;
    }
}