using System.Text.Json;

namespace InfrastructureCli.Rewriters;

public interface IRewriter
{
    JsonElement Rewrite(JsonElement jsonElement, IRewriter rootRewriter);
}