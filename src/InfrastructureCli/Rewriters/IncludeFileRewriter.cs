using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using InfrastructureCli.Extensions;
using InfrastructureCli.Services;

namespace InfrastructureCli.Rewriters
{
    internal sealed class IncludeFileRewriter : RewriterBase
    {
        private readonly string _currentPath;

        public IncludeFileRewriter(string currentPath)
        {
            _currentPath = currentPath;
        }
        
        protected override JsonElement RewriteObject(IReadOnlyDictionary<string, JsonElement> jsonProperties, IRewriter rootRewriter)
        {
            if (TryGetArgumentsElement(jsonProperties, "IncludeFile", out var argumentsElement) != true ||
                argumentsElement.ValueKind != JsonValueKind.Array)
            {
                return base.RewriteObject(jsonProperties, rootRewriter);
            }

            var jsonElements = argumentsElement
                .EnumerateArray()
                .ToArray();

            if (jsonElements.Any(jsonElement => jsonElement.ValueKind != JsonValueKind.String))
            {
                return base.RewriteObject(jsonProperties, rootRewriter);
            }

            var fileNameComponents = jsonElements
                .Select(jsonElement => jsonElement.GetString()!)
                .Prepend(_currentPath)
                .ToArray();

            var fileName = Path.Combine(fileNameComponents);
            
            var fileInfo = new FileInfo(fileName);

            var jsonElement = FileService.DeserializeFromFile<JsonElement>(fileInfo).Result;

            var augmentedRewriter = new ChainRewriter(new[]
            {
                new IncludeFileRewriter(fileInfo.DirectoryName!),
                rootRewriter
            });

            return augmentedRewriter.Rewrite(jsonElement);
        }
    }
}