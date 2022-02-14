using System.Text.Json;
using InfrastructureCli.Rewriters;

namespace InfrastructureCli.Extensions
{
    internal static class IRewriterExtensions
    {
        public static JsonElement Rewrite(this IRewriter rewriter, JsonElement jsonElement)
        {
            return rewriter.Rewrite(jsonElement, rewriter);
        }
    }
}