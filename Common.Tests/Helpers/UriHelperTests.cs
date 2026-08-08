using Common.Helpers;

namespace Common.Tests.Helpers;

public class UriHelperTests
{
    [Fact]
    public void AddsSingleQueryParameter()
    {
        UriBuilder uriBuilder = new("https://test.test");

        uriBuilder.AddQueryParameter("name", "value");

        Assert.Equal("https://test.test/?name=value&", uriBuilder.Uri.AbsoluteUri);
    }

    [Fact]
    public void QueryParametersAreAddedCorrectly()
    {
        UriBuilder uriBuilder = new("https://test.test");
        Assert.Equal("https://test.test/", uriBuilder.Uri.AbsoluteUri);
        uriBuilder.AddQueryParameter("name", "value");
        Assert.Equal("https://test.test/?name=value&", uriBuilder.Uri.AbsoluteUri);
        uriBuilder.AddQueryParameter("n", "v");
        Assert.Equal("https://test.test/?name=value&n=v&", uriBuilder.Uri.AbsoluteUri);
    }

    [Fact]
    public void ChainsMultipleParametersPreservingOrder()
    {
        UriBuilder uriBuilder = new("https://test.test");

        UriBuilder result = uriBuilder
            .AddQueryParameter("a", "1")
            .AddQueryParameter("b", "2")
            .AddQueryParameter("c", "3");

        Assert.Same(uriBuilder, result);
        Assert.Equal("https://test.test/?a=1&b=2&c=3&", uriBuilder.Uri.AbsoluteUri);
    }

    [Fact]
    public void EncodesSpacesInParameterValue()
    {
        UriBuilder uriBuilder = new("https://test.test");

        uriBuilder.AddQueryParameter("q", "hello world");

        Assert.Equal("https://test.test/?q=hello%20world&", uriBuilder.Uri.AbsoluteUri);
    }

    [Fact]
    public void EncodesNonAsciiCharactersInParameterValue()
    {
        UriBuilder uriBuilder = new("https://test.test");

        uriBuilder.AddQueryParameter("title", "Ataque à Titã");

        string result = uriBuilder.Uri.AbsoluteUri;
        Assert.DoesNotContain("à", result);
        Assert.DoesNotContain("ã", result);
        Assert.Contains("%C3%A0", result);
        Assert.Contains("%C3%A3", result);
    }
}