using Common.Tests;
using Extensions.Custom;
using Microsoft.AspNetCore.Http.HttpResults;
using Services.Manga.Features.CustomDownloaders;

namespace Services.Manga.Tests.Features.CustomDownloaders;

public class PostCustomDownloaderTestEndpointTests : TrangaTest
{
    private static SelectorDownloaderDefinition Definition() =>
        SelectorDownloaderJson.WeebCentralExample() with { Id = Guid.Empty };

    private static PostCustomDownloaderTestEndpoint.PostCustomDownloaderTestRequest Request(string step, string input, SelectorDownloaderDefinition? definition = null) =>
        new(definition ?? Definition(), step, input);

    [Theory]
    [InlineData("nonsense")]
    [InlineData("")]
    [InlineData("7")]
    public async Task Test_RejectsUnknownSteps(string step)
    {
        Results<Ok<StepTestResult>, BadRequest<string[]>> result = await PostCustomDownloaderTestEndpoint.Handle(Request(step, "x"), ct);

        BadRequest<string[]> badRequest = Assert.IsType<BadRequest<string[]>>(result.Result);
        Assert.Contains("Unknown step", Assert.Single(badRequest.Value!));
    }

    [Fact]
    public async Task Test_RequiresInput()
    {
        Results<Ok<StepTestResult>, BadRequest<string[]>> result = await PostCustomDownloaderTestEndpoint.Handle(Request("search", "  "), ct);

        Assert.IsType<BadRequest<string[]>>(result.Result);
    }

    [Fact]
    public async Task Test_OnlyValidatesGeneralSettingsAndTheTestedStep()
    {
        SelectorDownloaderDefinition onlySearch = Definition() with { Chapters = new ChaptersSpec(), Images = new ImagesSpec(), BaseUrl = "http://localhost:1" };

        // search is complete -> accepted (the request itself is refused later because localhost is a private host)
        Assert.IsType<Ok<StepTestResult>>((await PostCustomDownloaderTestEndpoint.Handle(Request("search", "x", onlySearch), ct)).Result);
        // chapters / images are still empty -> rejected before anything is requested
        Assert.IsType<BadRequest<string[]>>((await PostCustomDownloaderTestEndpoint.Handle(Request("chapters", "http://localhost:1/series/1", onlySearch), ct)).Result);
        Assert.IsType<BadRequest<string[]>>((await PostCustomDownloaderTestEndpoint.Handle(Request("images", "http://localhost:1/chapter/1", onlySearch), ct)).Result);
    }

    [Fact]
    public async Task Test_StepNamesAreCaseInsensitive()
    {
        SelectorDownloaderDefinition definition = Definition() with { BaseUrl = "http://localhost:1" };

        Results<Ok<StepTestResult>, BadRequest<string[]>> result = await PostCustomDownloaderTestEndpoint.Handle(Request("SEARCH", "x", definition), ct);

        Assert.IsType<Ok<StepTestResult>>(result.Result);
    }

    [Fact]
    public async Task Test_ReportsRefusedPrivateHostsInsideTheResult()
    {
        SelectorDownloaderDefinition definition = Definition() with { BaseUrl = "http://localhost:1" };

        Results<Ok<StepTestResult>, BadRequest<string[]>> result = await PostCustomDownloaderTestEndpoint.Handle(Request("search", "x", definition), ct);

        StepTestResult test = Assert.IsType<Ok<StepTestResult>>(result.Result).Value!;
        Assert.NotNull(test.Error);
        Assert.Contains("ALLOW_PRIVATE_SOURCE_HOSTS", test.Error);
        Assert.Equal("http://localhost:1/search/data?text=x&limit=32&offset=0&display_mode=Full%20Display", test.RequestUrl);
    }

    [Fact]
    public async Task Test_RejectsInvalidDefinitionsForTheStep()
    {
        SelectorDownloaderDefinition definition = Definition() with { Search = Definition().Search with { ItemSelector = "a[" } };

        Results<Ok<StepTestResult>, BadRequest<string[]>> result = await PostCustomDownloaderTestEndpoint.Handle(Request("search", "x", definition), ct);

        BadRequest<string[]> badRequest = Assert.IsType<BadRequest<string[]>>(result.Result);
        Assert.Contains(badRequest.Value!, e => e.Contains("Search result item selector"));
    }
}
