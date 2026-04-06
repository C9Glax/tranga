namespace Extensions.Tests;

public sealed class DownloadExtensionCollectionTests
{
    [Fact]
    public void UniqueExtensionIds()
    {
        Assert.Distinct(DownloadExtensionsCollection.Extensions.Select(e => e.Identifier));
    }
}