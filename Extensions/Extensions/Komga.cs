using Common.Helpers;
using Common.Settings;
using Extensions.Data;
using Komga.Client.Api;
using Komga.Client.Client;
using Komga.Client.Model;

namespace Extensions.Extensions;

public sealed class Komga(string baseUrl) : ILibraryExtension<KomgaSeries, KomgaBook, StringIdentifier>
{
    private static readonly HttpClientHandler Handler = new() { UseCookies = true, };

    private static readonly RequestClient KomgaRequestClient = new()
    {
        DefaultRequestHeaders = { { "X-API-Key", EnvVars.KomgaApiKey } }
    };

    private readonly LibrariesApi _librariesApi = new(KomgaRequestClient, baseUrl, Handler);
    private readonly SeriesApi _series = new(KomgaRequestClient, baseUrl, Handler);
    private readonly SeriesPosterApi _seriesPoster = new(KomgaRequestClient, baseUrl, Handler);

    public async Task<StringIdentifier> CreateTrangaLibrary(CancellationToken ct, string? rootDir = null)
    {
        LibraryDto result = await _librariesApi.AddLibraryAsync(
            new LibraryCreationDto(analyzeDimensions: true, convertToCbz: true, root: rootDir ?? "/tranga",
                scanDirectoryExclusions: [], emptyTrashAfterScan: true, hashFiles: true, hashPages: true,
                importComicInfoBook: true, importLocalArtwork: true, name: "Tranga", scanCbx: true, scanOnStartup: true,
                scanInterval: LibraryCreationDto.ScanIntervalEnum.HOURLY), ct);
        return result.Id;
    }

    public async Task<KomgaSeries[]> GetSeriesList(CancellationToken ct)
    {
        PageSeriesDto pageSeriesDto = await _series.GetSeriesAsync(new SeriesSearch(), true, cancellationToken: ct);
        return pageSeriesDto.Content.Select(s => new KomgaSeries(s.Id, s.Name, s.Metadata.Summary)).ToArray();
    }

    public Task UpdateSeriesMetadata(KomgaSeries series, CancellationToken ct)
    {
        SeriesMetadataUpdateDto dto = new(title: series.Name, summary: series.Summary);
        return _series.UpdateSeriesMetadataAsync(series.Id, dto, ct);
    }

    public Task UpdateSeriesPoster(StringIdentifier seriesId, TrangaImage poster, CancellationToken ct) =>
        _seriesPoster.AddUserUploadedSeriesThumbnailAsync(seriesId, new FileParameter(poster), selected: true, ct);

    public Task ScanLibrary(StringIdentifier libraryId, CancellationToken ct) =>
        _librariesApi.LibraryScanAsync(libraryId, cancellationToken: ct);
}

public sealed record KomgaSeries(StringIdentifier Id, string Name, string Summary) : ISeries<StringIdentifier>;

public sealed record KomgaBook(StringIdentifier Id, string FilePath, string Title) : IBook<StringIdentifier>;