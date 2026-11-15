using Common.Helpers;
using KomgaExtension = Extensions.Extensions.Komga;
using KavitaExtension = Extensions.Extensions.Kavita;
using Extensions.Extensions;

namespace Services.Libraries.Helpers;

/// <summary>A series on a remote library service.</summary>
internal sealed record LibrarySeries(string Id, string Name);

/// <summary>
/// Service-agnostic view of a library extension (Komga, Kavita, ...), so handlers, linking and metadata
/// sync do not depend on a concrete extension type.
/// </summary>
internal interface ILibraryClient
{
    Task ScanLibrary(string libraryId, CancellationToken ct);

    Task<LibrarySeries[]> GetSeriesList(string libraryId, CancellationToken ct);

    Task UpdateSeriesMetadata(string seriesId, string name, string summary, CancellationToken ct);

    Task UpdateSeriesPoster(string seriesId, string fileName, string contentType, TrangaImage poster, CancellationToken ct);
}

internal sealed class KomgaLibraryClient(KomgaExtension komga) : ILibraryClient
{
    public Task ScanLibrary(string libraryId, CancellationToken ct) => komga.ScanLibrary(libraryId, ct);

    public async Task<LibrarySeries[]> GetSeriesList(string libraryId, CancellationToken ct) =>
        (await komga.GetSeriesList(libraryId, ct)).Select(s => new LibrarySeries(s.Id, s.Name)).ToArray();

    public Task UpdateSeriesMetadata(string seriesId, string name, string summary, CancellationToken ct) =>
        komga.UpdateSeriesMetadata(new KomgaSeries(seriesId, name, summary), ct);

    public Task UpdateSeriesPoster(string seriesId, string fileName, string contentType, TrangaImage poster, CancellationToken ct) =>
        komga.UpdateSeriesPoster(seriesId, fileName, contentType, poster, ct);
}

internal sealed class KavitaLibraryClient(KavitaExtension kavita) : ILibraryClient
{
    public Task ScanLibrary(string libraryId, CancellationToken ct) => kavita.ScanLibrary(libraryId, ct);

    public async Task<LibrarySeries[]> GetSeriesList(string libraryId, CancellationToken ct) =>
        (await kavita.GetSeriesList(libraryId, ct)).Select(s => new LibrarySeries(s.Id, s.Name)).ToArray();

    public Task UpdateSeriesMetadata(string seriesId, string name, string summary, CancellationToken ct) =>
        kavita.UpdateSeriesMetadata(new KavitaSeries(seriesId, name, summary), ct);

    public Task UpdateSeriesPoster(string seriesId, string fileName, string contentType, TrangaImage poster, CancellationToken ct) =>
        kavita.UpdateSeriesPoster(seriesId, fileName, contentType, poster, ct);
}
