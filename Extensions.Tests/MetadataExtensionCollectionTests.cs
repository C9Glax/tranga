namespace Extensions.Tests;

public sealed class MetadataExtensionCollectionTests
{
    [Fact]
    public void UniqueExtensionIds()
    {
        Assert.Distinct(MetadataExtensionsCollection.Extensions.Select(e => e.Identifier));
    }
}