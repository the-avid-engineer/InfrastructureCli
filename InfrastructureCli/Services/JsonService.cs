using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace InfrastructureCli.Services
{
    public static class JsonService
    {
        private static readonly JsonSerializerOptions Indented = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters =
            {
                new JsonStringEnumConverter(),
            }
        };

        private static readonly JsonSerializerOptions Flat = new()
        {
            WriteIndented = false,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        public static async Task<T> DeserializeAsync<T>(Stream stream)
        {
            return (await JsonSerializer.DeserializeAsync<T>(stream, Indented))!;
        }

        public static async Task SerializeAsync<T>(T t, Stream stream)
        {
            await JsonSerializer.SerializeAsync(stream, t, Indented);
        }

        public static string Serialize<T>(T t)
        {
            return JsonSerializer.Serialize(t, Flat);
        }
    }
}
