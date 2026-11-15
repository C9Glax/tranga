using System.Net.Http.Json;
using Common.Helpers;
using Extensions.Data;
using Newtonsoft.Json.Linq;

namespace Extensions.Extensions;

/// <summary>
/// Kavita library extension. Authenticates with a Kavita "Auth Key" (User Settings -> Manage Auth Keys)
/// sent as the <c>x-api-key</c> header (Kavita 0.9.x and newer).
/// Kavita reads metadata from the ComicInfo.xml Tranga writes into every chapter, so metadata and poster
/// pushes are intentionally no-ops. Kavita libraries have to be created in Kavita itself; use
/// <see cref="GetLibraries"/> to let the user pick one.
/// </summary>
public sealed class Kavita : ILibraryExtension<KavitaSeries, KavitaBook, StringIdentifier>
{
    // FilterField.Libraries / FilterComparison.Equal / FilterCombination.And in Kavita's SeriesFilterV2Dto.
    private const int FilterFieldLibraries = 19;
    private const int FilterComparisonEqual = 0;
    private const int FilterCombinationAnd = 1;

    private readonly HttpClient _client;

    public Kavita(string baseUrl, string apiKey)
    {
        _client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
            DefaultRequestHeaders = { { "x-api-key", apiKey } }
        };
    }

    /// <summary>
    /// Kavita libraries cannot be created with sensible defaults from here, so Tranga binds to an existing one.
    /// </summary>
    public Task<StringIdentifier> CreateTrangaLibrary(CancellationToken ct, string? rootDir = null) =>
        throw new NotSupportedException("Kavita libraries have to be created in Kavita; pick an existing library instead.");

    /// <summary>
    /// Lists all libraries on the Kavita server. Also serves as an authenticated connection test: throws
    /// <see cref="HttpRequestException"/> if the server is unreachable or the Auth Key is rejected.
    /// </summary>
    public async Task<KavitaLibrary[]> GetLibraries(CancellationToken ct)
    {
        using HttpResponseMessage response = await _client.GetAsync("api/Library/libraries", ct);
        response.EnsureSuccessStatusCode();
        JArray libraries = JArray.Parse(await response.Content.ReadAsStringAsync(ct));
        return libraries
            .Select(l => new KavitaLibrary(l.Value<int>("id").ToString(), l.Value<string>("name") ?? string.Empty))
            .ToArray();
    }

    public Task<KavitaSeries[]> GetSeriesList(CancellationToken ct) => QuerySeries(null, ct);

    /// <summary>
    /// Fetches only the series belonging to the given Kavita library.
    /// </summary>
    public Task<KavitaSeries[]> GetSeriesList(StringIdentifier libraryId, CancellationToken ct) =>
        QuerySeries(libraryId, ct);

    private async Task<KavitaSeries[]> QuerySeries(string? libraryId, CancellationToken ct)
    {
        object body = new
        {
            statements = libraryId is null
                ? Array.Empty<object>()
                : [new { comparison = FilterComparisonEqual, field = FilterFieldLibraries, value = libraryId }],
            combination = FilterCombinationAnd,
            sortOptions = new { sortField = 1, isAscending = true },
            limitTo = 0
        };

        // PageSize 0 = unpaged in Kavita.
        using HttpResponseMessage response = await _client.PostAsJsonAsync("api/Series/all-v2?PageNumber=0&PageSize=0", body, ct);
        response.EnsureSuccessStatusCode();
        JArray series = JArray.Parse(await response.Content.ReadAsStringAsync(ct));
        return series
            .Select(s => new KavitaSeries(s.Value<int>("id").ToString(), s.Value<string>("name") ?? string.Empty, string.Empty))
            .ToArray();
    }

    /// <summary>No-op: Kavita imports metadata from the ComicInfo.xml inside the chapter files.</summary>
    public Task UpdateSeriesMetadata(KavitaSeries series, CancellationToken ct) => Task.CompletedTask;

    /// <summary>No-op: Kavita derives series covers from the chapter files.</summary>
    public Task UpdateSeriesPoster(StringIdentifier seriesId, string fileName, string contentType, TrangaImage poster, CancellationToken ct) =>
        Task.CompletedTask;

    public async Task ScanLibrary(StringIdentifier libraryId, CancellationToken ct)
    {
        using HttpResponseMessage response = await _client.PostAsync($"api/Library/scan?libraryId={Uri.EscapeDataString(libraryId)}&force=false", null, ct);
        response.EnsureSuccessStatusCode();
    }
}

public sealed record KavitaLibrary(string Id, string Name);

public sealed record KavitaSeries(StringIdentifier Id, string Name, string Summary) : ISeries<StringIdentifier>;

public sealed record KavitaBook(StringIdentifier Id, string Title) : IBook<StringIdentifier>;
