using API;

namespace Tests;

public class UtilsTest
{
    [Theory]
    [InlineData("https://localhost", "", "https://localhost")]
    [InlineData("https://localhost/", "", "https://localhost")]
    [InlineData("https://localhost", "wow", "https://localhost/wow")]
    [InlineData("https://localhost", "/wow", "https://localhost/wow")]
    [InlineData("https://localhost/", "wow", "https://localhost/wow")]
    [InlineData("https://localhost/", "/wow", "https://localhost/wow")]
    [InlineData("https://localhost/abc", "wow", "https://localhost/abc/wow")]
    [InlineData("https://localhost/abc", "/wow", "https://localhost/abc/wow")]
    [InlineData("https://localhost/abc/", "wow", "https://localhost/abc/wow")]
    [InlineData("https://localhost/abc/", "/wow", "https://localhost/abc/wow")]
    public void BuildUri_BuildsCorrectUri(string basePath, string relativePath, string fullPath)
    {
        Assert.Equal(new Uri(fullPath), Utils.BuildUri(basePath, relativePath));
    }
}