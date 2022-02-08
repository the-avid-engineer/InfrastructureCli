using System.Linq;
using System.Text.Json;

namespace InfrastructureCli.Rewriters
{
    public interface IRewriter
    {
        JsonElement Rewrite(JsonElement jsonElement, IRewriter rootRewriter);
    }

    internal sealed class ChainRewriter : IRewriter
    {
        public static readonly IRewriter Base = new ChainRewriter
        (
            new MapElementsRewriter(),
            new MapPropertiesRewriter(),
            new UsingAttributesRewriter(),
            new GetPropertyValueRewriter(),
            new SpreadElementsRewriter(),
            new SpreadPropertiesRewriter(),
            new SerializeRewriter()
        );
        
        private readonly IRewriter[] _rewriters;

        public ChainRewriter(params IRewriter[] rewriters)
        {
            _rewriters = rewriters;
        }

        public JsonElement Rewrite(JsonElement jsonElement, IRewriter rootRewriter)
        {
            foreach (var rewriter in _rewriters)
            {
                jsonElement = rewriter.Rewrite(jsonElement, rootRewriter);
            }
            
            return jsonElement;
        }
    }
}