using Extensions.Custom;

namespace Extensions.Tests.Custom;

public sealed class SelectorDownloaderDefinitionTests
{
    private static SelectorDownloaderDefinition Valid => SelectorDownloaderJson.WeebCentralExample() with { Id = Guid.NewGuid() };

    [Fact]
    public void WeebCentralExampleIsValid() => Assert.Empty(Valid.Validate());

    [Fact]
    public void MangaTownExampleIsValid() => Assert.Empty((SelectorDownloaderJson.MangaTownExample() with { Id = Guid.NewGuid() }).Validate());

    [Fact]
    public void ThePageListIsOptional() => Assert.True(Valid.Images.PageSelector.IsUnused);

    [Fact]
    public void ThePageListNeedsASelectorThatCompiles()
    {
        SelectorDownloaderDefinition definition = Valid;

        IReadOnlyList<string> noSelector = (definition with
        {
            Images = definition.Images with { PageSelector = new SelectorField { Attribute = "value" } }
        }).Validate();
        IReadOnlyList<string> badSelector = (definition with
        {
            Images = definition.Images with { PageSelector = new SelectorField { Selector = "div[" } }
        }).Validate();

        Assert.Contains(noSelector, e => e.Contains("Chapter page list"));
        Assert.Contains(badSelector, e => e.Contains("Chapter page list"));
    }

    [Fact]
    public void ThePageListSurvivesJsonRoundTrip()
    {
        SelectorDownloaderDefinition example = SelectorDownloaderJson.MangaTownExample();

        SelectorDownloaderDefinition? copy = SelectorDownloaderJson.Deserialize(SelectorDownloaderJson.Serialize(example));

        Assert.NotNull(copy);
        Assert.False(copy.Images.PageSelector.IsUnused);
        Assert.Equal(example.Images.PageSelector, copy.Images.PageSelector);
    }

    [Fact]
    public void ExampleWithoutIdIsInvalid() =>
        Assert.Contains("Id must be set.", (Valid with { Id = Guid.Empty }).Validate());

    [Fact]
    public void EmptyDefinitionReportsEveryRequiredPart()
    {
        IReadOnlyList<string> errors = new SelectorDownloaderDefinition { Id = Guid.NewGuid() }.Validate();

        Assert.Contains(errors, e => e.Contains("Name"));
        Assert.Contains(errors, e => e.Contains("Base URL"));
        Assert.Contains(errors, e => e.Contains("Search URL"));
        Assert.Contains(errors, e => e.Contains("Search result item selector"));
        Assert.Contains(errors, e => e.Contains("Chapter item selector"));
        Assert.Contains(errors, e => e.Contains("Chapter URL"));
        Assert.Contains(errors, e => e.Contains("Page image selector"));
    }

    [Fact]
    public void StepValidationOnlyChecksGeneralSettingsAndThatStep()
    {
        SelectorDownloaderDefinition onlySearch = Valid with { Chapters = new ChaptersSpec(), Images = new ImagesSpec() };

        Assert.Empty(onlySearch.Validate(SelectorDownloaderStep.Search));
        Assert.NotEmpty(onlySearch.Validate(SelectorDownloaderStep.Chapters));
        Assert.NotEmpty(onlySearch.Validate(SelectorDownloaderStep.Images));
        Assert.NotEmpty(onlySearch.Validate());
        // general problems are always reported
        Assert.NotEmpty((onlySearch with { Name = "" }).Validate(SelectorDownloaderStep.Search));
    }

    [Theory]
    [InlineData("ftp://example.com")]
    [InlineData("example.com")]
    [InlineData("/relative")]
    [InlineData("")]
    public void BaseUrlMustBeAbsoluteHttp(string baseUrl) =>
        Assert.Contains((Valid with { BaseUrl = baseUrl }).Validate(), e => e.Contains("Base URL"));

    [Theory]
    [InlineData(0)]
    [InlineData(0.05)]
    [InlineData(21)]
    [InlineData(double.NaN)]
    public void RequestsPerSecondIsLimited(double requestsPerSecond) =>
        Assert.Contains((Valid with { RequestsPerSecond = requestsPerSecond }).Validate(), e => e.Contains("Requests per second"));

    [Theory]
    [InlineData(0.1)]
    [InlineData(0.5)]
    [InlineData(20)]
    public void RequestsPerSecondInRangeIsAccepted(double requestsPerSecond) =>
        Assert.Empty((Valid with { RequestsPerSecond = requestsPerSecond }).Validate());

    [Fact]
    public void LanguagesAreChecked()
    {
        Assert.Contains((Valid with { Languages = [] }).Validate(), e => e.Contains("language"));
        Assert.Contains((Valid with { Languages = ["not-a-language-code-at-all"] }).Validate(), e => e.Contains("not a valid language"));
        Assert.Empty((Valid with { Languages = ["en", "pt-BR"] }).Validate());
    }

    [Theory]
    [InlineData("en")]
    [InlineData("en-us")]
    [InlineData("pt-BR")]
    [InlineData("zh-Hans")]
    [InlineData("es-419")]
    public void CommonLanguageCodesAreAccepted(string language) => Assert.Empty((Valid with { Languages = [language] }).Validate());

    [Theory]
    [InlineData("amazing")]
    [InlineData("wowthiscanbeinputhere")]
    [InlineData("english")]
    [InlineData("e")]
    [InlineData("en_US")]
    [InlineData("en us")]
    public void WordsThatAreNotLanguageCodesAreRejected(string language) =>
        Assert.Contains((Valid with { Languages = ["en", language] }).Validate(), e => e.Contains($"'{language}' is not a valid language"));

    [Theory]
    [InlineData("https://example.com/", "https://example.com")]
    [InlineData("  https://example.com///  ", "https://example.com")]
    [InlineData("https://example.com/en/", "https://example.com/en")]
    [InlineData("https://example.com", "https://example.com")]
    public void TheBaseUrlLosesWhitespaceAndTrailingSlashes(string baseUrl, string expected) =>
        Assert.Equal(expected, (Valid with { BaseUrl = baseUrl }).Normalized().BaseUrl);

    [Fact]
    public void NormalizingKeepsEverythingElse()
    {
        SelectorDownloaderDefinition definition = Valid;

        Assert.Equal(definition, definition.Normalized());
    }

    [Fact]
    public void AnUnparsableBaseUrlStaysInvalidAfterNormalizing()
    {
        Assert.Contains("Base URL", string.Join(' ', (Valid with { BaseUrl = "///" }).Normalized().Validate()));
    }

    [Theory]
    [InlineData("Host")]
    [InlineData("content-length")]
    [InlineData("bad header")]
    [InlineData("Bad:Header")]
    public void ForbiddenOrMalformedHeadersAreRejected(string header) =>
        Assert.NotEmpty((Valid with { Headers = new Dictionary<string, string> { [header] = "x" } }).Validate());

    [Fact]
    public void HeaderValuesCannotContainLineBreaks() =>
        Assert.NotEmpty((Valid with { Headers = new Dictionary<string, string> { ["X-Test"] = "a\r\nInjected: 1" } }).Validate());

    [Fact]
    public void CookieAndUserAgentHeadersAreAllowed() =>
        Assert.Empty((Valid with { Headers = new Dictionary<string, string> { ["Cookie"] = "a=b", ["User-Agent"] = "Mozilla/5.0" } }).Validate());

    [Fact]
    public void UnknownPlaceholdersAreReported()
    {
        SelectorDownloaderDefinition definition = Valid with
        {
            Search = Valid.Search with { UrlTemplate = "/search?q={querry}" },
            Chapters = Valid.Chapters with { UrlTemplate = "/series/{slug}/chapters" }
        };

        IReadOnlyList<string> errors = definition.Validate();

        Assert.Contains(errors, e => e.Contains("{querry}"));
        Assert.Contains(errors, e => e.Contains("{slug}") && e.Contains("{id}")); // lists what is available, incl. the pattern's named group
    }

    [Fact]
    public void NamedGroupsOfTheSeriesPatternCanBeUsedInTheChapterUrl() =>
        Assert.Empty((Valid with { Chapters = Valid.Chapters with { UrlTemplate = "/s/{id}/{url}" } }).Validate());

    [Fact]
    public void InvalidSeriesPatternIsReported() =>
        Assert.Contains((Valid with { SeriesUrlPattern = "(unclosed" }).Validate(), e => e.Contains("Series URL pattern"));

    [Fact]
    public void InvalidSelectorsAreReportedWithTheirField()
    {
        SelectorDownloaderDefinition definition = Valid with
        {
            Search = Valid.Search with { ItemSelector = "a[", Title = new SelectorField { Selector = "//div[" } },
            Images = Valid.Images with { ImageSelector = "img:nonsense(" }
        };

        IReadOnlyList<string> errors = definition.Validate();

        Assert.Contains(errors, e => e.StartsWith("Search result item selector is not a valid selector"));
        Assert.Contains(errors, e => e.StartsWith("Search result title is not a valid selector"));
        Assert.Contains(errors, e => e.StartsWith("Page image selector is not a valid selector"));
    }

    [Fact]
    public void InvalidRegexIsReported() =>
        Assert.Contains((Valid with { Chapters = Valid.Chapters with { Number = new SelectorField { Regex = "(" } } }).Validate(), e => e.Contains("Chapter number regex"));

    [Fact]
    public void OptionalFieldsMayBeUnused()
    {
        SelectorDownloaderDefinition definition = Valid with
        {
            Chapters = Valid.Chapters with { Number = new SelectorField(), Title = new SelectorField(), Date = new SelectorField(), NextPage = new SelectorField() }
        };
        Assert.Empty(definition.Validate());
    }

    [Fact]
    public void JsonRoundTrips()
    {
        SelectorDownloaderDefinition definition = Valid;

        SelectorDownloaderDefinition? parsed = SelectorDownloaderJson.Deserialize(SelectorDownloaderJson.Serialize(definition));

        Assert.NotNull(parsed);
        Assert.Equal(definition.Id, parsed.Id);
        Assert.Equal(definition.Search, parsed.Search);
        Assert.Equal(definition.Chapters, parsed.Chapters);
        Assert.Equal(definition.Images, parsed.Images);
        Assert.Equal(definition.SeriesUrlPattern, parsed.SeriesUrlPattern);
    }

    [Fact]
    public void JsonUsesCamelCaseAndToleratesMissingParts()
    {
        SelectorDownloaderDefinition? parsed = SelectorDownloaderJson.Deserialize("""{"name":"X","baseUrl":"https://x.test","search":{"itemSelector":"a"}}""");

        Assert.NotNull(parsed);
        Assert.Equal("X", parsed.Name);
        Assert.Equal("a", parsed.Search.ItemSelector);
        Assert.Equal("{url}", parsed.Chapters.UrlTemplate);
        Assert.Equal(["en"], parsed.Languages);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[1,2]")]
    [InlineData("")]
    public void DeserializeReturnsNullForGarbage(string json) => Assert.Null(SelectorDownloaderJson.Deserialize(json));
}
