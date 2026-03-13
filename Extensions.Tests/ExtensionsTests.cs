namespace Extensions.Tests;

public abstract class ExtensionsTests<T> where T : IExtension<T>, new()
{
    // ReSharper disable once InconsistentNaming
    private readonly IExtension<T> Extension = new T();
    
    /// <summary>
    /// Test checks that the <see cref="IExtension{T}.BaseUrl"/> is set
    /// </summary>
    [Fact]
    public void BaseUrlNotNull() => Assert.False(string.IsNullOrEmpty(Extension.BaseUrl));

    /// <summary>
    /// Test checks that the <see cref="IExtension{T}.SupportedLanguages"/> has at least one supported Language
    /// </summary>
    [Fact]
    public void AtLeastOneSupportedLanguage() => Assert.NotEmpty(Extension.SupportedLanguages);
}