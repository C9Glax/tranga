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
    public void EveryExampleHasItsOwnCoverSelectorForSeriesAddedByUrl()
    {
        IReadOnlyList<SelectorDownloaderDefinition> examples = SelectorDownloaderJson.Examples();

        // nothing is guessed: what a site's series page offers (a preview image, an image in the info block, a field of
        // the API answer) is written down by the source itself
        Assert.All(examples, example => Assert.True(example.Series.HasCover(example.Format), example.Name));
        Assert.Equal(examples.Count, examples.Select(e => e.Series.Cover.Selector).Distinct().Count());
    }

    [Fact]
    public void TheSeriesSectionIsOptional()
    {
        SelectorDownloaderDefinition definition = Valid;

        Assert.Empty((definition with { Series = new SeriesSpec() }).Validate());
        Assert.False(new SeriesSpec().HasCover(SourceFormat.Html));
        // a blank template is the series URL itself
        Assert.Empty((definition with { Series = definition.Series with { UrlTemplate = "" } }).Validate());
    }

    [Fact]
    public void TheSeriesCoverNeedsASelectorThatCompiles()
    {
        SelectorDownloaderDefinition definition = Valid;

        IReadOnlyList<string> errors = (definition with
        {
            Series = definition.Series with { Cover = new SelectorField { Selector = "img[", Attribute = "src" } }
        }).Validate();

        Assert.Contains(errors, e => e.Contains("Series cover is not a valid selector"));
    }

    [Fact]
    public void TheSeriesPageUrlMayOnlyUseThePlaceholdersOfTheSeriesUrl()
    {
        SelectorDownloaderDefinition definition = Valid;

        // {id} is a group of the series URL pattern of the example, {query} only exists in the search
        Assert.Empty((definition with { Series = definition.Series with { UrlTemplate = "/series/{id}/info" } }).Validate());
        Assert.Contains((definition with { Series = definition.Series with { UrlTemplate = "/series/{query}" } }).Validate(),
            e => e.Contains("Series page URL") && e.Contains("{query}"));
    }

    [Fact]
    public void TheSeriesSectionSurvivesJsonRoundTrip()
    {
        SelectorDownloaderDefinition example = SelectorDownloaderJson.MangaUnoExample();

        SelectorDownloaderDefinition? copy = SelectorDownloaderJson.Deserialize(SelectorDownloaderJson.Serialize(example));

        Assert.NotNull(copy);
        Assert.Equal(example.Series, copy.Series);
        // a stored definition from before the section existed has none, which means no cover of the website
        SelectorDownloaderDefinition? stored = SelectorDownloaderJson.Deserialize("""{ "name": "Old", "baseUrl": "https://example.com" }""");
        Assert.NotNull(stored);
        Assert.False(stored.Series.HasCover(stored.Format));
    }

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

    // The id is not part of what clients send (the server assigns it), so the datatype does not ask for it.
    // An extension cannot be created without one, see SelectorDownloadExtensionTests.AnExtensionCannotBeCreatedWithoutAnId.
    [Fact]
    public void ModelValidationDoesNotAskForAnId() => Assert.Empty((Valid with { Id = Guid.Empty }).Validate());

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
    public void SettingsValidateThemselvesWithoutAnySection()
    {
        SelectorDownloaderSettings settings = new() { Name = "Site", BaseUrl = "https://example.com" };

        Assert.Empty(settings.Validate());
        Assert.Contains("Name is required.", (settings with { Name = "" }).Validate());
        Assert.Contains("Base URL must be an absolute http(s) URL.", (settings with { BaseUrl = "nope" }).Validate());
    }

    [Fact]
    public void SectionsValidateThemselves()
    {
        SelectorDownloaderDefinition valid = Valid;

        Assert.Empty(valid.Search.Validate("Search", SourceFormat.Html));
        Assert.Empty(valid.Chapters.Validate("Chapters", SourceFormat.Html, valid.SeriesUrlGroups()));
        Assert.Empty(valid.Images.Validate("Images", SourceFormat.Html));
        Assert.NotEmpty(new SearchSpec().Validate("Search", SourceFormat.Html));
        Assert.NotEmpty(new ChaptersSpec().Validate("Chapters", SourceFormat.Html, []));
        Assert.NotEmpty(new ImagesSpec().Validate("Images", SourceFormat.Html));
    }

    [Fact]
    public void AChapterListUrlMayUseTheGroupsOfTheSeriesPattern()
    {
        ChaptersSpec chapters = Valid.Chapters with { UrlTemplate = "/series/{id}/chapters" };

        Assert.Empty(chapters.Validate("Chapters", SourceFormat.Html, ["id"]));
        Assert.Contains(chapters.Validate("Chapters", SourceFormat.Html, []), r => r.ErrorMessage!.Contains("unknown placeholder '{id}'"));
    }

    [Fact]
    public void TheDefinitionValidatesItsSettingsAndAllSectionsAtOnceAndNamesTheMembers()
    {
        SelectorDownloaderDefinition broken = Valid with { BaseUrl = "nope", Search = new SearchSpec(), Images = new ImagesSpec() };

        List<System.ComponentModel.DataAnnotations.ValidationResult> results = broken
            .Validate(new System.ComponentModel.DataAnnotations.ValidationContext(broken)).ToList();

        string[] members = results.SelectMany(r => r.MemberNames).Distinct().ToArray();
        Assert.Contains("BaseUrl", members);
        Assert.Contains("Search.UrlTemplate", members);
        Assert.Contains("Search.ItemSelector", members);
        Assert.Contains("Images.ImageSelector", members);
        Assert.DoesNotContain(members, m => m.StartsWith("Chapters"));
        // plain Validate() is the same list as messages
        Assert.Equal(results.Select(r => r.ErrorMessage), broken.Validate());
    }

    [Fact]
    public void SettingsAreTheFlatPartOfTheDefinitionJson()
    {
        string json = SelectorDownloaderJson.Serialize(Valid);

        SelectorDownloaderSettings? settings = System.Text.Json.JsonSerializer.Deserialize<SelectorDownloaderSettings>(json, SelectorDownloaderJson.Options);

        Assert.NotNull(settings);
        Assert.Equal(Valid.BaseUrl, settings.BaseUrl);
        Assert.Equal(Valid.Name, settings.Name);
        Assert.Contains("\"search\"", json);
        Assert.Contains("\"baseUrl\"", json);
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

    // ------------------------------------------------------------------------------------------------ JSON sources

    private static SelectorDownloaderDefinition ValidJson => SelectorDownloaderJson.MangaUnoExample() with { Id = Guid.NewGuid() };

    [Fact]
    public void MangaUnoExampleIsValidAndReadsJson()
    {
        Assert.Empty(ValidJson.Validate());
        Assert.Equal(SourceFormat.Json, ValidJson.Format);
    }

    [Fact]
    public void EveryExampleIsOfferedAndValid() =>
        Assert.All(SelectorDownloaderJson.Examples(), example => Assert.Empty((example with { Id = Guid.NewGuid() }).Validate()));

    [Fact]
    public void ADefinitionWithoutAFormatReadsHtml() =>
        Assert.Equal(SourceFormat.Html, SelectorDownloaderJson.Deserialize("""{"name":"X","baseUrl":"https://x.test"}""")!.Format);

    [Fact]
    public void TheFormatIsReadFromItsLowerCaseName() => Assert.Equal(SourceFormat.Json,
        SelectorDownloaderJson.Deserialize("""{"name":"X","baseUrl":"https://x.test","format":"json"}""")!.Format);

    [Fact]
    public void TheFormatIsWrittenInLowerCase()
    {
        Assert.Contains("\"format\":\"json\"", SelectorDownloaderJson.Serialize(ValidJson));
        Assert.Contains("\"format\":\"html\"", SelectorDownloaderJson.Serialize(Valid));
    }

    [Theory]
    [InlineData("\"xml\"")]
    [InlineData("\"JSON\"")] // the names are exactly html and json
    [InlineData("1")]
    public void AnUnknownFormatIsNotADefinition(string format) => Assert.Null(
        SelectorDownloaderJson.Deserialize($$"""{"name":"X","baseUrl":"https://x.test","format":{{format}}}"""));

    [Fact]
    public void AJsonSourceIsCheckedAsPathsNotSelectors()
    {
        SelectorDownloaderDefinition definition = ValidJson;

        // a CSS selector is not a path, and a path with a filter is not supported
        IReadOnlyList<string> css = (definition with { Search = definition.Search with { ItemSelector = "div.result > a" } }).Validate();
        IReadOnlyList<string> filter = (definition with { Chapters = definition.Chapters with { ItemSelector = "chapters[?(@.language=='en')]" } }).Validate();
        // the same paths are fine selectors for nothing in HTML either way, but "a.b[0]" is a valid CSS selector: only the format decides
        IReadOnlyList<string> asHtml = (Valid with { Search = Valid.Search with { ItemSelector = "data[0]" } }).Validate();

        Assert.Contains(css, e => e.Contains("Search result item selector is not a valid JSON path"));
        Assert.Contains(filter, e => e.Contains("Chapter item selector is not a valid JSON path"));
        Assert.Contains(asHtml, e => e.Contains("Search result item selector is not a valid selector"));
    }

    [Fact]
    public void AJsonSourceNeedsAChapterNumberAndIgnoresAttributes()
    {
        SelectorDownloaderDefinition definition = ValidJson;

        // an HTML chapter can be searched for a number, a JSON object cannot
        IReadOnlyList<string> noNumber = (definition with { Chapters = definition.Chapters with { Number = new SelectorField() } }).Validate();
        // there are no attributes in JSON, so a field with only an attribute reads nothing
        IReadOnlyList<string> onlyAttribute = (definition with { Search = definition.Search with { Url = new SelectorField { Attribute = "href" } } }).Validate();

        Assert.Contains("Chapter number is required.", noNumber);
        Assert.Contains("Search result URL is required.", onlyAttribute);
        Assert.Empty((Valid with { Chapters = Valid.Chapters with { Number = new SelectorField() } }).Validate());
    }

    [Fact]
    public void TheGroupsOfTheChapterUrlPatternCanBeUsedInThePagesUrl()
    {
        SelectorDownloaderDefinition definition = ValidJson;

        IReadOnlyList<string> withoutPattern = (definition with { Images = definition.Images with { UrlPattern = null } }).Validate();
        IReadOnlyList<string> badPattern = (definition with { Images = definition.Images with { UrlPattern = "(" } }).Validate();

        Assert.Equal(["id"], definition.Images.UrlGroups());
        Assert.Contains(withoutPattern, e => e.Contains("Chapter pages URL uses unknown placeholder '{id}'"));
        Assert.Contains(badPattern, e => e.Contains("Chapter URL pattern is not a valid regular expression"));
    }

    [Fact]
    public void AJsonSourceSurvivesJsonRoundTrip()
    {
        SelectorDownloaderDefinition definition = ValidJson;

        SelectorDownloaderDefinition? parsed = SelectorDownloaderJson.Deserialize(SelectorDownloaderJson.Serialize(definition));

        Assert.NotNull(parsed);
        Assert.Equal(SourceFormat.Json, parsed.Format);
        Assert.Equal(definition.Images, parsed.Images);
        Assert.Equal(definition.Chapters, parsed.Chapters);
    }
}
