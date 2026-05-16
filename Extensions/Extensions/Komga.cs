using Common.Helpers;
using Common.Settings;
using Extensions.Data;
using Komga.Client.Api;
using Komga.Client.Client;
using Komga.Client.Model;

namespace Extensions.Extensions;

public sealed class Komga(Guid id, string name, string baseUrl) : ILibraryExtension<KomgaSeries, KomgaBook, KomgaIdentifier>
{
    public Guid Identifier { get; init; } = id;
    public string Name { get; init; } = name;
    public string BaseUrl { get; init; } = baseUrl;

    private static readonly HttpClientHandler Handler = new ()
    {
        UseCookies = true,
    };

    private static readonly RequestClient KomgaRequestClient = new()
    {
        DefaultRequestHeaders =
        {
            {
                "X-API-Key", EnvVars.KomgaApiKey
            }
        }
    };

    private readonly LibrariesApi _libraries = new(KomgaRequestClient, baseUrl, Handler);

    private readonly SeriesApi _series = new(KomgaRequestClient, baseUrl, Handler);

    private readonly ImportApi _import = new(KomgaRequestClient, baseUrl, Handler);

    private readonly BooksApi _books = new(KomgaRequestClient, baseUrl, Handler);

    private readonly SeriesPosterApi _seriesPoster = new(KomgaRequestClient, baseUrl, Handler);
    
    private readonly BookPosterApi _bookPoster = new(KomgaRequestClient, baseUrl, Handler);

    public async Task<KomgaSeries[]> GetSeriesList(CancellationToken ct)
    {
        PageSeriesDto pageSeriesDto = await _series.GetSeriesAsync(new SeriesSearch(), true, cancellationToken: ct);
        return pageSeriesDto.Content.Select(s => new KomgaSeries(s.Id, s.Name, s.Metadata.Summary)).ToArray();
    }

    public Task AddSeries(KomgaSeries series, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public async Task<KomgaSeries> GetSeries(KomgaIdentifier id, CancellationToken ct)
    {
        SeriesDto seriesDto = await _series.GetSeriesByIdAsync(id, ct);
        return new KomgaSeries(seriesDto.Id, seriesDto.Name, seriesDto.Metadata.Summary);
    }

    public Task UpdateSeries(KomgaSeries series, CancellationToken ct)
    {
        SeriesMetadataUpdateDto dto = new (title: series.Name, summary: series.Summary);
        return _series.UpdateSeriesMetadataAsync(series.Id, dto, ct);
    }

    public Task UpdatePoster(KomgaIdentifier seriesId, TrangaImage poster, CancellationToken ct) =>
        _seriesPoster.AddUserUploadedSeriesThumbnailAsync(seriesId, new FileParameter(poster), selected: true, ct);

    public Task DeleteSeries(KomgaIdentifier seriesId, CancellationToken ct) =>
        _series.DeleteSeriesFileAsync(seriesId, ct);

    public Task AddBook(KomgaIdentifier seriesId, KomgaBook book, CancellationToken ct) => _import.ImportBooksAsync(
        new BookImportBatchDto([new BookImportDto(seriesId: seriesId, sourceFile: book.FilePath)],
            BookImportBatchDto.CopyModeEnum.HARDLINK), cancellationToken: ct);

    public async Task<KomgaBook[]> GetBooks(KomgaIdentifier seriesId, CancellationToken ct)
    {
        PageBookDto pageBookDto = await _books.GetBooksAsync(
            new BookSearch(new AnyOfBookAllOfAnyOf(new SeriesId(new SeriesIdAllOfSeriesId(new Is("Is", seriesId))))),
            unpaged: true, cancellationToken: ct);
        return pageBookDto.Content.Select(b => new KomgaBook(b.Id, b.Url, b.Name)).ToArray();
    }

    public Task UpdateBook(KomgaIdentifier bookId, KomgaBook book, CancellationToken ct)
        => _books.UpdateBookMetadataAsync(bookId, new BookMetadataUpdateDto(title: book.Title), ct);

    public Task UpdateBookCover(KomgaIdentifier bookId, TrangaImage cover, CancellationToken ct) =>
        _bookPoster.AddUserUploadedBookThumbnailAsync(bookId, new FileParameter(cover), selected: true, ct);

    public Task DeleteBook(KomgaIdentifier bookId, CancellationToken ct) => _books.DeleteBookFileAsync(bookId, ct);
}

public sealed class KomgaIdentifier(string id) : IIdentifier
{
    private string Id { get; set; } = id;
    
    public static implicit operator string(KomgaIdentifier id) => id.Id;
    public static implicit operator KomgaIdentifier(string str) => new (str);
}

public sealed record KomgaSeries(KomgaIdentifier Id, string Name, string Summary) : ISeries<KomgaIdentifier>;

public sealed record KomgaBook(KomgaIdentifier Id, string FilePath, string Title) : IBook<KomgaIdentifier>;