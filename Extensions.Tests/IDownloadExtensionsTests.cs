using DownloadExtensions;

namespace Extensions.Tests;

public abstract class IDownloadExtensionsTests<T> where T : IDownloadExtension<T>, new()
{
    // ReSharper disable once InconsistentNaming
    private readonly IDownloadExtension<T> _downloadExtension = new T();

    /// <summary>
    /// Test checks that the <see cref="IDownloadExtension{T}.BaseUrl"/> is set
    /// </summary>
    [Fact]
    public void IdentifierSet()
    {
        Assert.NotEqual(Guid.Empty, _downloadExtension.Identifier);
        Assert.NotEqual(Guid.AllBitsSet, _downloadExtension.Identifier);
    }
    
    /// <summary>
    /// Test checks that the <see cref="IDownloadExtension{T}.Name"/> is set
    /// </summary>
    [Fact]
    public void NameSet() => Assert.False(string.IsNullOrEmpty(_downloadExtension.Name));
    
    /// <summary>
    /// Test checks that the <see cref="IDownloadExtension{T}.BaseUrl"/> is set
    /// </summary>
    [Fact]
    public void BaseUrlSet() => Assert.False(string.IsNullOrEmpty(_downloadExtension.BaseUrl));

    /// <summary>
    /// Test checks that the <see cref="IDownloadExtension{T}.SupportedLanguages"/> has at least one supported Language
    /// </summary>
    [Fact]
    public void AtLeastOneSupportedLanguage() => Assert.NotEmpty(_downloadExtension.SupportedLanguages);
}