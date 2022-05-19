using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using InfrastructureCli.Rewriters;
using InfrastructureCli.Services;
using Shouldly;
using Xunit;

namespace InfrastructureCli.Tests.JsonElementExtensions;

public class JsonElementExtensionsTests
{
    [Theory]
    [InlineData("GetAttributeValueDefined")]
    [InlineData("GetAttributeValueUndefined")]
    [InlineData("IncludeFile")]
    [InlineData("IncludeRawFile")]
    [InlineData("MapElements")]
    [InlineData("MapProperties")]
    [InlineData("GetPropertyValueDefined")]
    [InlineData("GetPropertyValueUndefined")]
    [InlineData("SpreadElements")]
    [InlineData("SpreadProperties")]
    [InlineData("Serialize")]
    [InlineData("IntProduct")]
    [InlineData("Precedence")]
    [InlineData("GetMacroDefined")]
    [InlineData("GetMacroUndefined")]
    [InlineData("Precedence2")]
    public async Task RewriteFixtureTests(string fixtureName)
    {
        // ARRANGE

        var currentPath = Path.Combine("Fixtures", "JsonElementExtensions", fixtureName);

        var inputFileName = Path.Combine(currentPath, "input.json");
        var expectedOutputFileName = Path.Combine(currentPath, "expectedOutput.json");

        await using var inputFile = File.OpenRead(inputFileName);
        await using var expectedOutputFile = File.OpenRead(expectedOutputFileName);

        var input = await JsonService.DeserializeAsync<JsonElement>(inputFile);
        var expectedOutput = await JsonService.DeserializeAsync<JsonElement>(expectedOutputFile);

        // ACT

        var rootRewriter = RootRewriter.Create
        (
            new(),
            new(),
            new(),
            new(),
            currentPath,
            "us-east-1"
        );
            
        var actualOutput = rootRewriter.Rewrite(input);

        var formattedExpectedOutput = JsonService.Serialize(expectedOutput);
        var formattedActualOutput = JsonService.Serialize(actualOutput);

        // ASSERT

        formattedActualOutput.ShouldBe(formattedExpectedOutput);
    }
}