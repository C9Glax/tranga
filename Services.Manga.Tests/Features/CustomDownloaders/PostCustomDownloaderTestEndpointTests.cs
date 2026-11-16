using System.ComponentModel.DataAnnotations;
using Common.Tests;
using Extensions.Custom;
using Microsoft.AspNetCore.Http.HttpResults;
using Services.Manga.Features.CustomDownloaders;
using static Services.Manga.Features.CustomDownloaders.PostCustomDownloaderTestEndpoint;

namespace Services.Manga.Tests.Features.CustomDownloaders;

/// <summary>
/// The request of the test endpoint validates itself (the framework asks it before the endpoint runs); these tests ask it
/// the same way and check what the endpoint does with a valid request.
/// </summary>
public class PostCustomDownloaderTestEndpointTests : TrangaTest
{
    private static SelectorDownloaderDefinition Example() => SelectorDownloaderJson.WeebCentralExample();

    /// <summary>Only the general settings of the example (no sections), as the frontend sends them.</summary>
    private static SelectorDownloaderSettings Settings(string baseUrl = "https://weebcentral.com") => new()
    {
        Name = Example().Name,
        BaseUrl = baseUrl,
        RequestsPerSecond = Example().RequestsPerSecond,
        SeriesUrlPattern = Example().SeriesUrlPattern
    };

    private static PostCustomDownloaderTestRequest SearchRequest(string step = "search", string input = "x", string baseUrl = "https://weebcentral.com") => new()
    {
        Settings = Settings(baseUrl), Step = step, Input = input, Search = Example().Search
    };

    private static List<ValidationResult> Validate(PostCustomDownloaderTestRequest request) => request.Validate(new ValidationContext(request)).ToList();

    #region The request validates itself

    [Theory]
    [InlineData("nonsense")]
    [InlineData("")]
    [InlineData("7")]
    public void Request_RejectsUnknownSteps(string step)
    {
        ValidationResult result = Assert.Single(Validate(SearchRequest(step)));

        Assert.Contains("Unknown step", result.ErrorMessage);
        Assert.Equal(["Step"], result.MemberNames);
    }

    [Fact]
    public void Request_RequiresInput()
    {
        ValidationResult result = Assert.Single(Validate(SearchRequest(input: "  ")));

        Assert.Equal(["Input"], result.MemberNames);
    }

    [Fact]
    public void Request_OnlyNeedsTheSectionOfItsStep()
    {
        // settings + search section, nothing else: fine for the search ...
        Assert.Empty(Validate(SearchRequest()));
        // ... but the same request cannot test the chapters or the pages
        Assert.Equal(["Chapters"], Assert.Single(Validate(SearchRequest("chapters", "https://weebcentral.com/series/1"))).MemberNames);
        Assert.Equal(["Images"], Assert.Single(Validate(SearchRequest("images", "https://weebcentral.com/chapters/1"))).MemberNames);
    }

    [Fact]
    public void Request_StepNamesAreCaseInsensitive() => Assert.Empty(Validate(SearchRequest("SEARCH")));

    [Fact]
    public void Request_IgnoresSectionsThatAreNotTested()
    {
        // an invalid chapters section does not matter while the search is tried
        PostCustomDownloaderTestRequest request = SearchRequest() with { Chapters = new ChaptersSpec() };

        Assert.Empty(Validate(request));
    }

    [Fact]
    public void Request_ChecksTheSectionOfItsStep()
    {
        PostCustomDownloaderTestRequest request = SearchRequest() with { Search = Example().Search with { ItemSelector = "a[" } };

        ValidationResult result = Assert.Single(Validate(request));

        Assert.Contains("Search result item selector", result.ErrorMessage);
        Assert.Equal(["Search.ItemSelector"], result.MemberNames);
    }

    [Fact]
    public void Request_ChecksTheSectionInTheFormatOfTheSettings()
    {
        SelectorDownloaderDefinition uno = SelectorDownloaderJson.MangaUnoExample();
        SelectorDownloaderSettings json = new() { Name = uno.Name, BaseUrl = uno.BaseUrl, Format = SourceFormat.Json, SeriesUrlPattern = uno.SeriesUrlPattern };
        PostCustomDownloaderTestRequest request = new() { Settings = json, Step = "search", Input = "x", Search = uno.Search };

        // "data" is a path; the same section is not usable as CSS selectors (a field without attribute or selector...)
        Assert.Empty(Validate(request));
        ValidationResult result = Assert.Single(Validate(request with { Search = uno.Search with { ItemSelector = "div.result > a" } }));
        Assert.Contains("not a valid JSON path", result.ErrorMessage);
        Assert.Equal(["Search.ItemSelector"], result.MemberNames);
        // and a JSON chapter needs its number, which an HTML chapter does not
        Assert.Contains(Validate(new PostCustomDownloaderTestRequest
        {
            Settings = json, Step = "chapters", Input = "https://manga.uno/m/x", Chapters = uno.Chapters with { Number = new SelectorField() }
        }), r => r.ErrorMessage == "Chapter number is required.");
    }

    [Fact]
    public void Request_AnImagesTestMayUseTheGroupsOfItsOwnChapterUrlPattern()
    {
        SelectorDownloaderDefinition uno = SelectorDownloaderJson.MangaUnoExample();
        SelectorDownloaderSettings json = new() { Name = uno.Name, BaseUrl = uno.BaseUrl, Format = SourceFormat.Json };
        PostCustomDownloaderTestRequest request = new() { Settings = json, Step = "images", Input = "https://manga.uno/r/x/1", Images = uno.Images };

        // the example's pages URL is /api/chapter/{id}, {id} is a group of the chapter URL pattern of the same section
        Assert.Empty(Validate(request));
        Assert.NotEmpty(Validate(request with { Images = uno.Images with { UrlPattern = null } }));
    }

    [Fact]
    public void Request_AChaptersTestMayUseTheGroupsOfTheSettingsSeriesPattern()
    {
        PostCustomDownloaderTestRequest request = new()
        {
            Settings = Settings(), Step = "chapters", Input = "https://weebcentral.com/series/1", Chapters = Example().Chapters
        };

        // the example's chapter list URL is /series/{id}/full-chapter-list, {id} is a group of the series pattern in the settings
        Assert.Empty(Validate(request));
        Assert.NotEmpty(Validate(request with { Settings = Settings() with { SeriesUrlPattern = null } }));
    }

    [Fact]
    public void Request_TheSettingsValidateThemselves()
    {
        // not the request's business: the settings are validated by their own datatype (the framework does that as well)
        Assert.Contains("Base URL must be an absolute http(s) URL.", SearchRequest(baseUrl: "nope").Settings.Validate());
    }

    #endregion

    #region The endpoint

    [Fact]
    public async Task Test_ReportsRefusedPrivateHostsInsideTheResult()
    {
        Ok<StepTestResult> result = await PostCustomDownloaderTestEndpoint.Handle(SearchRequest(baseUrl: "http://localhost:1"), ct);

        Assert.NotNull(result.Value!.Error);
        Assert.Contains("ALLOW_PRIVATE_SOURCE_HOSTS", result.Value.Error);
        Assert.Equal("http://localhost:1/search/data?text=x&limit=32&offset=0&display_mode=Full%20Display", result.Value.RequestUrl);
    }

    [Fact]
    public async Task Test_StepNamesAreCaseInsensitive()
    {
        Ok<StepTestResult> result = await PostCustomDownloaderTestEndpoint.Handle(SearchRequest("SEARCH", baseUrl: "http://localhost:1"), ct);

        Assert.NotNull(result.Value!.RequestUrl);
    }

    [Fact]
    public async Task Test_IgnoresSectionsOfOtherSteps()
    {
        // found end to end: the request does not check a section of another step, so the engine must not see it either
        PostCustomDownloaderTestRequest request = SearchRequest(baseUrl: "http://localhost:1") with { Chapters = new ChaptersSpec(), Images = new ImagesSpec() };

        Ok<StepTestResult> result = await PostCustomDownloaderTestEndpoint.Handle(request, ct);

        Assert.NotNull(result.Value!.RequestUrl);
    }

    [Fact]
    public async Task Test_AssignsAnIdItself()
    {
        // the frontend sends no id; the test works without one
        Assert.Equal(Guid.Empty, SearchRequest().Settings.Id);

        Ok<StepTestResult> result = await PostCustomDownloaderTestEndpoint.Handle(SearchRequest(baseUrl: "http://localhost:1"), ct);

        Assert.NotNull(result.Value);
    }

    #endregion
}
