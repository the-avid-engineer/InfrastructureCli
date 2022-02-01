using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using InfrastructureCli.Extensions;
using InfrastructureCli.Services;
using Shouldly;
using Xunit;

namespace InfrastructureCli.Tests.JsonElementExtensions
{
    public class JsonElementExtensionsTests
    {
        [Theory]
        [InlineData("GetAttributeValueDefined")]
        [InlineData("GetAttributeValueUndefined")]
        [InlineData("GetPropertyValueDefined")]
        [InlineData("GetPropertyValueUndefined")]
        [InlineData("MapArray")]
        [InlineData("MapObject")]
        [InlineData("Serialize")]
        [InlineData("SpreadArray")]
        [InlineData("SpreadObject")]
        public static async Task RewriteFixtureTests(string fixtureName)
        {
            // ARRANGE

            var inputFileName = Path.Combine("Fixtures", "JsonElementExtensions", fixtureName, "input.json");
            var expectedOutputFileName = Path.Combine("Fixtures", "JsonElementExtensions", fixtureName, "expectedOutput.json");

            await using var inputFile = File.OpenRead(inputFileName);
            await using var expectedOutputFile = File.OpenRead(expectedOutputFileName);

            var input = await JsonService.DeserializeAsync<JsonElement>(inputFile);
            var expectedOutput = await JsonService.DeserializeAsync<JsonElement>(expectedOutputFile);

            // ACT

            var actualOutput = input
                .RewriteGetAttributeValues<dynamic>(new()
                {
                    ["Foo"] = "Bar"
                })
                .RewriteMaps()
                .RewriteSpreads()
                .RewriteGetPropertyValues()
                .RewriteSerializes();

            var formattedExpectedOutput = JsonService.Serialize(expectedOutput);
            var formattedActualOutput = JsonService.Serialize(actualOutput);

            // ASSERT

            formattedActualOutput.ShouldBe(formattedExpectedOutput);
        }
    }
}
