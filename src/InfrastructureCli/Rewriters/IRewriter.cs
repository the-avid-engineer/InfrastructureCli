using System.Text.Json;

namespace InfrastructureCli.Rewriters;

public interface IRewriter
{
    JsonElement Rewrite(JsonElement jsonElement, IRootRewriter rootRewriter);
}

public interface IRootRewriter
{
    string CurrentPath { get; }
    JsonElement Rewrite(JsonElement jsonElement);
    IRootRewriter WithCurrentPath(string currentPath);
    IRootRewriter PrependToBottomUp(IRewriter rewriter);
    IRootRewriter PrependToTopDown(IRewriter rewriter);
}