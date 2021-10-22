using System.IO;
using System.Threading.Tasks;

namespace InfrastructureCli.Services
{
    public static class FileService
    {
        public static async Task<T> DeserializeFromFile<T>(FileInfo fileInfo)
        {
            await using var file = fileInfo.OpenRead();

            return await JsonService.DeserializeAsync<T>(file);
        }

        public static async Task SerializeToFile<T>(T t, FileInfo fileInfo)
        {
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }

            await using var file = fileInfo.OpenWrite();

            await JsonService.SerializeAsync(t, file);

            await file.FlushAsync();
        }
    }
}
