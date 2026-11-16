using Extensions.Custom;
using Microsoft.AspNetCore.Http.HttpResults;
using Services.Manga.Features.CustomDownloaders;

namespace Services.Manga.Tests.Features.CustomDownloaders;

public class GetCustomDownloaderExamplesEndpointTests
{
    [Fact]
    public void Examples_AreValidOnceTheyGetAnId()
    {
        Ok<List<SelectorDownloaderDefinition>> result = GetCustomDownloaderExamplesEndpoint.Handle();

        Assert.NotEmpty(result.Value!);
        Assert.All(result.Value!, example =>
        {
            Assert.Equal(Guid.Empty, example.Id);
            Assert.Empty((example with { Id = Guid.NewGuid() }).Validate());
        });
        Assert.Contains(result.Value!, example => example.BaseUrl == "https://weebcentral.com");
        Assert.Contains(result.Value!, example => example.BaseUrl == "https://www.mangatown.com");
        // one of them reads the JSON of an API, so the form can show what that looks like
        Assert.Contains(result.Value!, example => example is { BaseUrl: "https://manga.uno", Format: SourceFormat.Json });
    }
}
