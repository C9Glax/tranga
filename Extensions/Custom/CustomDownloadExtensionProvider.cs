namespace Extensions.Custom;

/// <summary>
/// Provides the user-defined selector sources. The definitions come from wherever the host service stores them
/// (the manga service's database), supplied through <paramref name="loader"/>.
/// </summary>
/// <param name="loader">Returns the currently stored definitions</param>
/// <param name="options">Defaults to the environment</param>
public sealed class CustomDownloadExtensionProvider(
    Func<CancellationToken, Task<IReadOnlyList<SelectorDownloaderDefinition>>> loader,
    SelectorDownloaderOptions? options = null) : IDownloadExtensionProvider
{
    /// <inheritdoc />
    public string Name => "Custom sources";

    /// <inheritdoc />
    public async Task<IReadOnlyList<IDownloadExtension>> LoadAsync(CancellationToken ct)
    {
        List<IDownloadExtension> extensions = [];
        foreach (SelectorDownloaderDefinition definition in await loader(ct))
        {
            try
            {
                extensions.Add(new SelectorDownloadExtension(definition, options));
            }
            catch (ArgumentException)
            {
                // A stored definition that no longer validates is skipped instead of breaking every other source.
            }
        }

        return extensions;
    }
}
