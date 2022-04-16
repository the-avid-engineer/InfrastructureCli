using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using InfrastructureCli.Models;
using InfrastructureCli.Services;
using Shouldly;
using Xunit;

namespace InfrastructureCli.Tests.Services;

public class FileServiceTests
{
    private static async Task Generic_GivenFixtureAndType_ThenCanDeserializeFromFile<T>(string fixtureName) where T : class
    {
        // ARRANGE

        var inputFileName = new FileInfo(Path.Combine("Fixtures", "FileService", fixtureName, "input.json"));

        // ACT

        var resultTask = FileService.DeserializeFromFile<T>(inputFileName);

        await Should.NotThrowAsync(resultTask);

        resultTask.Result.ShouldNotBeNull();
    }
    
    [Theory]
    [InlineData("ConfigurationsFile", typeof(ConfigurationsFile))]
    public Task GivenFixtureAndType_ThenCanDeserializeFromFile(string fixtureName, Type deserializedType)
    {
        return (Task)GetType()
            .GetMethod(nameof(Generic_GivenFixtureAndType_ThenCanDeserializeFromFile), ~BindingFlags.Public)!
            .MakeGenericMethod(deserializedType)
            .Invoke(null, new object[] { fixtureName });
    }
}