using System.Text;
using Common.Helpers;
using Extensions.Data;
using Komga.Client.Api;
using Komga.Client.Client;
using Komga.Client.Model;

namespace Extensions.Extensions;

public sealed class Komga : ILibraryExtension<KomgaSeries, KomgaBook, StringIdentifier>
{
    private readonly HttpClientHandler _handler = new() { UseCookies = true, };

    private readonly RequestClient _komgaRequestClient;

    private readonly LibrariesApi _librariesApi;
    private readonly SeriesApi _series;
    private readonly SeriesPosterApi _seriesPoster;

    public Komga(string baseUrl, string apiKey)
    {
        baseUrl = baseUrl.TrimEnd('/');

        _komgaRequestClient = new RequestClient
        {
            DefaultRequestHeaders = { { "X-API-Key", apiKey } }
        };

        _librariesApi = new LibrariesApi(_komgaRequestClient, baseUrl, _handler);
        _series = new SeriesApi(_komgaRequestClient, baseUrl, _handler);
        _seriesPoster = new SeriesPosterApi(_komgaRequestClient, baseUrl, _handler);
    }

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

    /// <summary>
    /// Mints a new, revocable Komga API key for the given user via a one-time Basic-Auth call.
    /// The password is never stored; only the resulting key is meant to be persisted by the caller.
    /// </summary>
    public static async Task<string> MintApiKey(string baseUrl, string username, string password, CancellationToken ct)
    {
        baseUrl = baseUrl.TrimEnd('/');

        HttpClientHandler handler = new() { UseCookies = true, };

        RequestClient requestClient = new()
        {
            DefaultRequestHeaders =
            {
                { "Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")) }
            }
        };

        APIKeysApi apiKeysApi = new(requestClient, baseUrl, handler);
        ApiKeyDto result = await apiKeysApi.CreateApiKeyForCurrentUserAsync(new ApiKeyRequestDto(comment: "Tranga"), ct);
        return result.Key;
    }
}

public sealed record KomgaSeries(StringIdentifier Id, string Name, string Summary) : ISeries<StringIdentifier>;

public sealed record KomgaBook(StringIdentifier Id, string FilePath, string Title) : IBook<StringIdentifier>;