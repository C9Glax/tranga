using Common.Helpers;

namespace Extensions.Tests;

// ReSharper disable once InconsistentNaming
public abstract class ExtensionTests<T> : TestContext where T : IExtension, new()
{
    // ReSharper disable once InconsistentNaming
    protected readonly T _extension = new T();

    /// <summary>
    /// Test checks that the <see cref="IDownloadExtension.Identifier"/> is set
    /// </summary>
    [Fact]
    public void IdentifierSet()
    {
        Assert.NotEqual(Guid.Empty, _extension.Identifier);
        Assert.NotEqual(Guid.AllBitsSet, _extension.Identifier);
    }
    
    /// <summary>
    /// Test checks that the <see cref="IDownloadExtension.Name"/> is set
    /// </summary>
    [Fact]
    public void NameSet() => Assert.False(string.IsNullOrEmpty(_extension.Name));
    
    /// <summary>
    /// Test checks that the <see cref="IDownloadExtension.BaseUrl"/> is set
    /// </summary>
    [Fact]
    public void BaseUrlSet() => Assert.False(string.IsNullOrEmpty(_extension.BaseUrl));

}