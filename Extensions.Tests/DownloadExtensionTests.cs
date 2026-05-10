namespace Extensions.Tests;

public abstract class DownloadExtensionTests<T> : ExtensionTests<T> where T : IDownloadExtension, new()
{
    /// <summary>
    /// Test checks that the <see cref="IDownloadExtension.SupportedLanguages"/> has at least one supported Language
    /// </summary>
    [Fact]
    public void AtLeastOneSupportedLanguage() => Assert.NotEmpty(_extension.SupportedLanguages);
}