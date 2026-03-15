using Common.Helpers;

namespace MetadataExtensions.Tests;

// ReSharper disable once InconsistentNaming
public abstract class IMetadataExtensionTests<T> : TestContext where T : IMetadataExtension<T>, new()
{
    // ReSharper disable once InconsistentNaming
    protected readonly T _metadataExtension = new T();
    
    /// <summary>
    /// Test checks that the <see cref="IMetadataExtension{T}.Name"/> is set
    /// </summary>
    [Fact]
    public void NameSet() => Assert.False(string.IsNullOrEmpty(_metadataExtension.Name));
    
    /// <summary>
    /// Test checks that the <see cref="IMetadataExtension{T}.BaseUrl"/> is set
    /// </summary>
    [Fact]
    public void BaseUrlSet() => Assert.False(string.IsNullOrEmpty(_metadataExtension.BaseUrl));
}