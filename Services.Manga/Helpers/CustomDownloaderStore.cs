using Extensions.Custom;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;

namespace Services.Manga.Helpers;

/// <summary>Connects the stored custom (selector based) download sources to the download extension registry.</summary>
public static class CustomDownloaderStore
{
    /// <summary>
    /// Creates the provider that loads the stored custom sources from the manga database. Register it with
    /// <c>DownloadExtensionsCollection.RegisterProvider</c> in every service that downloads or searches.
    /// </summary>
    /// <param name="services">Service provider the database context is resolved from (a scope is created per load)</param>
    public static CustomDownloadExtensionProvider CreateProvider(IServiceProvider services) => new(ct => LoadAsync(services, ct));

    private static async Task<IReadOnlyList<SelectorDownloaderDefinition>> LoadAsync(IServiceProvider services, CancellationToken ct)
    {
        using IServiceScope scope = services.CreateScope();
        MangaContext context = scope.ServiceProvider.GetRequiredService<MangaContext>();
        List<DbCustomDownloader> rows = await context.CustomDownloaders.AsNoTracking().ToListAsync(ct);
        return rows.Select(ToDefinition).OfType<SelectorDownloaderDefinition>().ToList();
    }

    /// <summary>Parses a stored source. The row's key wins over whatever id the JSON carries. Null if the JSON is unusable.</summary>
    internal static SelectorDownloaderDefinition? ToDefinition(DbCustomDownloader row) =>
        SelectorDownloaderJson.Deserialize(row.DefinitionJson) is { } definition ? definition with { Id = row.CustomDownloaderId } : null;
}
