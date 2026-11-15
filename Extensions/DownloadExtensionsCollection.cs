using Common.Datatypes;
using Common.Settings;
using Extensions.Data;

namespace Extensions;

/// <summary>
/// Registry of all available <see cref="IDownloadExtension"/>s, fed by one or more
/// <see cref="IDownloadExtensionProvider"/>s. Each service process keeps its own copy.
/// </summary>
public static class DownloadExtensionsCollection
{
    private static readonly Lock ProvidersLock = new();
    private static readonly List<IDownloadExtensionProvider> Providers = [new BuiltInDownloadExtensionProvider()];
    private static volatile IDownloadExtension[] _extensions = LoadBuiltIn();

    // Built-in extensions are available immediately, without anyone having to call RefreshAsync first.
    private static IDownloadExtension[] LoadBuiltIn() =>
        [.. new BuiltInDownloadExtensionProvider().LoadAsync(CancellationToken.None).GetAwaiter().GetResult()];

    /// <summary>Every download extension currently available.</summary>
    public static IDownloadExtension[] Extensions => _extensions;

    /// <summary>
    /// Adds a provider. Its extensions become available after the next <see cref="RefreshAsync"/>.
    /// </summary>
    public static void RegisterProvider(IDownloadExtensionProvider provider)
    {
        lock (ProvidersLock)
            Providers.Add(provider);
    }

    /// <summary>
    /// Re-queries all providers and atomically replaces the set of available extensions.
    /// <para>
    /// Best-effort: a provider that throws contributes nothing for this refresh but never takes the caller down.
    /// Extensions whose identifier is already taken by an earlier provider are ignored.
    /// </para>
    /// </summary>
    /// <returns>The number of extensions now registered.</returns>
    public static async Task<int> RefreshAsync(CancellationToken ct)
    {
        IDownloadExtensionProvider[] providers;
        lock (ProvidersLock)
            providers = [.. Providers];

        List<IDownloadExtension> all = [];
        foreach (IDownloadExtensionProvider provider in providers)
        {
            try
            {
                foreach (IDownloadExtension extension in await provider.LoadAsync(ct))
                    if (all.All(e => e.Identifier != extension.Identifier))
                        all.Add(extension);
            }
            catch (Exception) when (!ct.IsCancellationRequested)
            {
                // A broken provider must not make the other providers' extensions disappear.
            }
        }

        IDownloadExtension[] previous = _extensions;
        _extensions = [.. all];

        // Extensions that own resources (HTTP clients) are replaced by fresh instances on every refresh. Dispose the
        // old ones after a grace period so operations that already started with them can finish.
        IDisposable[] replaced = previous.Where(extension => !all.Contains(extension)).OfType<IDisposable>().ToArray();
        if (replaced.Length > 0)
            _ = Task.Delay(TimeSpan.FromMinutes(2), CancellationToken.None).ContinueWith(_ =>
            {
                foreach (IDisposable disposable in replaced)
                    disposable.Dispose();
            }, TaskScheduler.Default);

        return all.Count;
    }

    public static IDownloadExtension? GetExtension(Guid extensionId) => Extensions.FirstOrDefault(e => e.Identifier == extensionId);

    public static List<MangaInfo> SearchAll(SearchQuery query, CancellationToken ct) => Search(query, Extensions, ct);

    public static List<MangaInfo> Search(SearchQuery searchQuery, IDownloadExtension[] extensions, CancellationToken ct)
    {
        if (searchQuery.Language is null)
            searchQuery = searchQuery with { Language = Settings.DownloadLanguage };

        List<Task<List<MangaInfo>?>> tasks = extensions.Select(e => SearchSafe(e, searchQuery, ct)).ToList();

        Task.WaitAll(tasks, ct);

        return tasks
            .Where(t => t is { IsCompletedSuccessfully: true, Result: not null })
            .SelectMany(t => t.Result!).ToList();
    }

    /// <summary>An extension that throws contributes no results instead of failing the whole search.</summary>
    private static async Task<List<MangaInfo>?> SearchSafe(IDownloadExtension extension, SearchQuery query, CancellationToken ct)
    {
        try
        {
            return await extension.SearchDownload(query, ct);
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            return null;
        }
    }
}
