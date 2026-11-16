using System.Text.Json;
using Extensions.Custom;

namespace Extensions.Tests.Custom;

public sealed class JsonPathEvaluatorTests
{
    private const string Json = """
        {
          "data": [
            { "id": 1, "slug": "one", "title": "One", "english_title": "", "tags": ["a", "b"], "cover": null, "odd.name": "dotted" },
            { "id": 2, "slug": "two", "title": "Two", "english_title": "Second", "tags": [], "nested": { "deep": { "value": 2.50 } } }
          ],
          "manga": { "slug": "one", "adult": false, "pages": ["p1", "p2", "p3"] },
          "total": 2
        }
        """;

    private static JsonElement Root
    {
        get
        {
            using JsonDocument document = JsonDocument.Parse(Json);
            return document.RootElement.Clone();
        }
    }

    private static List<string?> Texts(string path) => JsonPathEvaluator.SelectAll(Root, path).Select(JsonPathEvaluator.ScalarText).ToList();

    [Theory]
    [InlineData("data")]
    [InlineData("data[*]")]
    [InlineData("$.data")]
    [InlineData("$.data[*]")]
    [InlineData("$['data']")]
    public void AListIsItsElementsHoweverItIsWritten(string path) => Assert.Equal(2, JsonPathEvaluator.SelectAll(Root, path).Count);

    [Fact]
    public void PropertiesAreWalkedWithDots()
    {
        Assert.Equal(["one"], Texts("manga.slug"));
        Assert.Equal(["2.50"], Texts("data[1].nested.deep.value")); // a number as it is written
        Assert.Equal(["false"], Texts("manga.adult"));
        Assert.Equal(["2"], Texts("total"));
    }

    [Fact]
    public void APropertyOfAListIsThePropertyOfEachElement() => Assert.Equal(["one", "two"], Texts("data.slug"));

    [Fact]
    public void ElementsCanBePickedByPositionFromEitherEnd()
    {
        Assert.Equal(["p1"], Texts("manga.pages[0]"));
        Assert.Equal(["p3"], Texts("manga.pages[-1]"));
        Assert.Equal(["p1", "p2", "p3"], Texts("manga.pages"));
        Assert.Empty(Texts("manga.pages[7]"));
    }

    [Fact]
    public void AlternativesAreCollectedInTheOrderTheyAreWritten() =>
        Assert.Equal(["Second", "Two"], JsonPathEvaluator
            .SelectAll(Root.GetProperty("data")[1], "english_title, title").Select(JsonPathEvaluator.ScalarText));

    [Fact]
    public void TheDollarAloneIsTheValueItself() => Assert.Equal(["p2"], JsonPathEvaluator
        .SelectAll(Root.GetProperty("manga").GetProperty("pages")[1], "$").Select(JsonPathEvaluator.ScalarText));

    [Fact]
    public void NamesWithDotsAreWrittenInBrackets() => Assert.Equal(["dotted"], Texts("data[0]['odd.name']"));

    [Fact]
    public void WhatIsNotThereSelectsNothing()
    {
        Assert.Empty(Texts("nope"));
        Assert.Empty(Texts("data[0].cover")); // null
        Assert.Empty(Texts("total.more"));
        Assert.Empty(Texts("data[1].tags"));
    }

    [Fact]
    public void EmptyStringsObjectsAndListsHaveNoText()
    {
        Assert.Null(JsonPathEvaluator.ScalarText(Root.GetProperty("data")[0].GetProperty("english_title")));
        Assert.Null(JsonPathEvaluator.ScalarText(Root.GetProperty("manga")));
        Assert.Null(JsonPathEvaluator.ScalarText(Root.GetProperty("data")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("$")]
    [InlineData("data")]
    [InlineData("$.data[*].title")]
    [InlineData("a.b[0].c[-1]")]
    [InlineData("english_title, title")]
    [InlineData("['a,b'].c")]
    [InlineData("data.*")]
    public void TryCompileAcceptsValidPaths(string path) => Assert.Null(JsonPathEvaluator.TryCompile(path));

    [Theory]
    [InlineData("data[")]
    [InlineData("data..title")]
    [InlineData("data.")]
    [InlineData("data[?(@.id==1)]")]
    [InlineData("data[1:2]")]
    [InlineData("a,,b")]
    [InlineData("$data")]
    [InlineData("div > a")]
    public void TryCompileRejectsWhatIsNotSupported(string path) => Assert.NotNull(JsonPathEvaluator.TryCompile(path));
}
