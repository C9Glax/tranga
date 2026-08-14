using Common.Datatypes;
using Common.Settings;
using Extensions.Data;
using Extensions.Extensions;
using Extensions.Extensions.Suwayomi;

namespace Extensions;

public static class DownloadExtensionsCollection
{
    /// <summary>Extensions compiled into Tranga. Fixed for the lifetime of the process.</summary>
    private static readonly IDownloadExtension[] BuiltIn = BuildBuiltInExtensions();

    /// <summary>
    /// Extensions backed by the Suwayomi sidecar, one per installed source. Replaced wholesale by
    /// <see cref="RefreshSidecarExtensionsAsync"/> whenever the set of installed extensions changes.
    /// </summary>
    private static volatile IDownloadExtension[] _sidecar = [];

    /// <summary>Every download extension currently available, built-in and sidecar-backed alike.</summary>
    public static IDownloadExtension[] Extensions => [.. BuiltIn, .. _sidecar];

    private static IDownloadExtension[] BuildBuiltInExtensions()
    {
        List<IDownloadExtension> extensions = [new MangaDex(), new AsuraScans(), new MangaPlus()];
        if (WeebCentral.IsAvailable)
            extensions.Add(new WeebCentral());
        return [.. extensions];
    }

    /// <summary>
    /// Re-reads the sources installed on the Suwayomi sidecar and republishes them as download extensions.
    /// <para>
    /// Best-effort by design: when the sidecar is switched off, unreachable or erroring, the previously discovered set
    /// is left in place and no exception escapes. Callers are startup hooks, the sources-changed event handler and a
    /// periodic task, none of which should be able to take a service down.
    /// </para>
    /// </summary>
    /// <returns>The number of sidecar-backed extensions now registered.</returns>
    public static async Task<int> RefreshSidecarExtensionsAsync(CancellationToken ct)
    {
        if (!SuwayomiSource.IsAvailable)
        {
            _sidecar = [];
            return 0;
        }

        IDownloadExtension[] discovered = await SuwayomiSource.DiscoverAsync(ct);

        // DiscoverAsync answers with an empty array both for "no sources installed" and for "sidecar unreachable".
        // Only the former should clear the list, so an unreachable sidecar is distinguished by probing it first.
        if (discovered.Length == 0 && await SuwayomiClient.GetAboutAsync(ct) is null)
            return _sidecar.Length;

        _sidecar = discovered;
        return discovered.Length;
    }

    public static IDownloadExtension? GetExtension(Guid extensionId) => Extensions.FirstOrDefault(e => e.Identifier == extensionId);

    public static List<MangaInfo> SearchAll(SearchQuery query, CancellationToken ct) => Search(query, Extensions, ct);

    public static List<MangaInfo> Search(SearchQuery searchQuery, IDownloadExtension[] extensions, CancellationToken ct)
    {
        if (searchQuery.Language is null)
            searchQuery = searchQuery with { Language = Settings.DownloadLanguage };

        List<Task<List<MangaInfo>?>> tasks = extensions.Select(e => e.SearchDownload(searchQuery, ct)).ToList();

        Task.WaitAll(tasks, ct);

        List<MangaInfo> ret = tasks
            .Where(t => t is { IsCompleted: true, Result: not null })
            .SelectMany(t => t.Result!).ToList();

        return ret;
    }

}
