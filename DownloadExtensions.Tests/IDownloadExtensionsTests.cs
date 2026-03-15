using Common.Helpers;

namespace DownloadExtensions.Tests;

// ReSharper disable once InconsistentNaming
public abstract class IDownloadExtensionsTests<T> : TestContext where T : IDownloadExtension, new()
{
    // ReSharper disable once InconsistentNaming
    protected readonly T _downloadExtension = new T();

    /// <summary>
    /// Test checks that the <see cref="IDownloadExtension.Identifier"/> is set
    /// </summary>
    [Fact]
    public void IdentifierSet()
    {
        Assert.NotEqual(Guid.Empty, _downloadExtension.Identifier);
        Assert.NotEqual(Guid.AllBitsSet, _downloadExtension.Identifier);
    }
    
    /// <summary>
    /// Test checks that the <see cref="IDownloadExtension.Name"/> is set
    /// </summary>
    [Fact]
    public void NameSet() => Assert.False(string.IsNullOrEmpty(_downloadExtension.Name));
    
    /// <summary>
    /// Test checks that the <see cref="IDownloadExtension.BaseUrl"/> is set
    /// </summary>
    [Fact]
    public void BaseUrlSet() => Assert.False(string.IsNullOrEmpty(_downloadExtension.BaseUrl));

    /// <summary>
    /// Test checks that the <see cref="IDownloadExtension.SupportedLanguages"/> has at least one supported Language
    /// </summary>
    [Fact]
    public void AtLeastOneSupportedLanguage() => Assert.NotEmpty(_downloadExtension.SupportedLanguages);
}