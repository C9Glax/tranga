using DownloadExtensions.Helpers;

namespace DownloadExtensions.Tests.Helpers;

public class UriHelperTests
{
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
}