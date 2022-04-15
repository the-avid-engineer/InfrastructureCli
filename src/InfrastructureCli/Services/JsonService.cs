using System.IO;
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
        
        #if NETCOREAPP3_1
        IgnoreNullValues = true,
        #else
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        #endif
        
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