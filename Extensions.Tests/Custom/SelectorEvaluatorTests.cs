using AngleSharp.Dom;
using Extensions.Custom;

namespace Extensions.Tests.Custom;

public sealed class SelectorEvaluatorTests
{
    private static readonly Uri PageUrl = new("https://example.com/series/1/");

    private const string Html = """
        <html><body>
        <ul id="list">
          <li class="item" data-id="a"><a href="/chapters/1">Chapter 1</a><time datetime="2026-01-02T03:04:05Z">Jan 2</time></li>
          <li class="item" data-id="b"><a href="chapters/2?x=1#frag">  Chapter   2
              - The  Title </a><time datetime="2026-01-03T00:00:00Z">Jan 3</time></li>
          <li class="item skip"><a href="javascript:void(0)">Nope</a></li>
        </ul>
        <picture><source srcset="https://cdn.example.com/normal/1.webp 2x, https://cdn.example.com/small/1.webp 1x"><img src="/fallback.jpg"></picture>
        <img data-src="//cdn.example.com/lazy.png" src="/placeholder.gif">
        </body></html>
        """;

    private static IDocument Document => SelectorEvaluator.Parse(Html);

    private static List<INode> Items(string selector) => SelectorEvaluator.SelectAll(Document, selector, relative: false);

    [Theory]
    [InlineData("li.item", 3)]
    [InlineData("li:not(.skip)", 2)] // :not() is not supported by every CSS engine
    [InlineData("li:has(time)", 2)]
    [InlineData("a:contains(Chapter)", 2)]
    [InlineData("li[data-id~=a]", 1)]
    [InlineData("ul > li:last-child", 1)]
    [InlineData("//li", 3)]
    [InlineData("//li[@data-id]", 2)]
    [InlineData("(//li)[1]", 1)]
    [InlineData("//li/a/@href", 3)]
    [InlineData("./body/ul/li", 3)] // relative XPath from the root element
    [InlineData("div.missing", 0)]
    public void SelectAllFindsMatchingNodes(string selector, int expected) => Assert.Equal(expected, Items(selector).Count);

    [Theory]
    [InlineData("li.item", false)]
    [InlineData("//li", true)]
    [InlineData("./a", true)]
    [InlineData("../a", true)]
    [InlineData("(//li)[1]", true)]
    [InlineData(" //li", true)]
    public void IsXPathDetectsXPath(string selector, bool expected) => Assert.Equal(expected, SelectorEvaluator.IsXPath(selector));

    [Theory]
    [InlineData("li.item")]
    [InlineData("//li[@data-id='a']")]
    [InlineData("a:not(.x)")]
    public void TryCompileAcceptsValidSelectors(string selector) => Assert.Null(SelectorEvaluator.TryCompile(selector));

    [Theory]
    [InlineData("li[")]
    [InlineData("a:nonsense(")]
    [InlineData("//li[")]
    public void TryCompileRejectsInvalidSelectors(string selector) => Assert.NotNull(SelectorEvaluator.TryCompile(selector));

    [Fact]
    public void TextIsNormalized()
    {
        INode second = Items("li.item")[1];
        Assert.Equal("Chapter 2 - The Title", SelectorEvaluator.ExtractText(second, new SelectorField { Selector = "a" }));
    }

    [Fact]
    public void EmptySelectorReadsFromTheItemItself()
    {
        INode first = Items("li.item a")[0];
        Assert.Equal("/chapters/1", SelectorEvaluator.ExtractText(first, new SelectorField { Attribute = "href" }));
    }

    [Fact]
    public void UnusedFieldYieldsNothing()
    {
        INode item = Items("li.item")[0];
        Assert.Null(SelectorEvaluator.ExtractText(item, new SelectorField()));
        Assert.Null(SelectorEvaluator.ExtractUrl(item, new SelectorField(), PageUrl));
    }

    [Fact]
    public void AttributeCanBeReadFromCss()
    {
        INode item = Items("li.item")[0];
        Assert.Equal("2026-01-02T03:04:05Z", SelectorEvaluator.ExtractText(item, new SelectorField { Selector = "time", Attribute = "datetime" }));
    }

    [Fact]
    public void XPathAttributeSuffixSelectsTheAttribute()
    {
        INode item = Items("li.item")[0];
        Assert.Equal("2026-01-02T03:04:05Z", SelectorEvaluator.ExtractText(item, new SelectorField { Selector = ".//time/@datetime" }));
    }

    [Fact]
    public void AbsoluteXPathIsRestrictedToTheItem()
    {
        // "Copy XPath" in browsers yields "//..." - evaluated for an item it must not escape the item.
        INode item = Items("li.item")[1];
        Assert.Equal("Chapter 2 - The Title", SelectorEvaluator.ExtractText(item, new SelectorField { Selector = "//a" }));
    }

    [Fact]
    public void AttributeFallbackListUsesTheFirstNonEmptyValue()
    {
        INode picture = Items("picture")[0];
        Assert.Equal("/fallback.jpg", SelectorEvaluator.ExtractText(picture, new SelectorField { Selector = "img", Attribute = "data-src, src" }));

        INode lazy = SelectorEvaluator.SelectAll(Document, "img[data-src]", relative: false)[0];
        Assert.Equal("//cdn.example.com/lazy.png", SelectorEvaluator.ExtractText(lazy, new SelectorField { Attribute = "data-src,src" }));
    }

    [Fact]
    public void FirstMatchingNodeWithAValueWins()
    {
        INode picture = Items("picture")[0];
        // <source> comes first in the document and has a srcset
        string? value = SelectorEvaluator.ExtractText(picture, new SelectorField { Selector = "source, img", Attribute = "srcset,src" });
        Assert.StartsWith("https://cdn.example.com/normal/1.webp", value);
    }

    [Theory]
    [InlineData("Chapter 12.5 - Title", @"Chapter\s+(\d+(?:\.\d+)?)", null, "12.5")]
    [InlineData("Chapter 12.5 - Title", @"\d+", null, "12")] // no group -> whole match
    [InlineData("Extra", @"Chapter\s+(\d+)", null, null)] // no match -> missing
    [InlineData("a/small/b/small/c", "/small/", "/normal/", "a/normal/b/normal/c")]
    public void RegexExtractsOrReplaces(string value, string regex, string? replacement, string? expected)
    {
        IDocument document = SelectorEvaluator.Parse($"<p>{value}</p>");
        string? actual = SelectorEvaluator.ExtractText(document.QuerySelector("p")!, new SelectorField { Selector = "", Regex = regex, Replacement = replacement });
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EmptyReplacementMeansExtractNotReplace()
    {
        IDocument document = SelectorEvaluator.Parse("<p>Chapter 7</p>");
        Assert.Equal("7", SelectorEvaluator.ExtractText(document.QuerySelector("p")!, new SelectorField { Regex = @"(\d+)", Replacement = "" }));
    }

    [Fact]
    public void InvalidOrRunawayRegexCountsAsNoMatch()
    {
        IDocument document = SelectorEvaluator.Parse("<p>" + new string('a', 40) + "!</p>");
        Assert.Null(SelectorEvaluator.ExtractText(document.QuerySelector("p")!, new SelectorField { Regex = "(a+)+$" }));
    }

    [Theory]
    [InlineData("/chapters/1", "https://example.com/chapters/1")]
    [InlineData("chapters/2?x=1#frag", "https://example.com/series/1/chapters/2?x=1")]
    [InlineData("//cdn.example.com/lazy.png", "https://cdn.example.com/lazy.png")]
    [InlineData("https://other.example.org/a b.png", "https://other.example.org/a%20b.png")]
    [InlineData("https://cdn.example.com/normal/1.webp 2x, https://cdn.example.com/small/1.webp 1x", "https://cdn.example.com/normal/1.webp")]
    [InlineData("https://cdn.example.com/a.webp 400w", "https://cdn.example.com/a.webp")]
    [InlineData("javascript:void(0)", null)]
    [InlineData("data:image/png;base64,AAAA", null)]
    [InlineData("mailto:a@b.c", null)]
    [InlineData("   ", null)]
    public void ResolveUrl(string value, string? expected) => Assert.Equal(expected, SelectorEvaluator.ResolveUrl(value, PageUrl));

    [Fact]
    public void ExtractUrlResolvesRelativeLinks()
    {
        INode item = Items("li.item")[0];
        Assert.Equal("https://example.com/chapters/1", SelectorEvaluator.ExtractUrl(item, new SelectorField { Selector = "a", Attribute = "href" }, PageUrl));
    }
}
