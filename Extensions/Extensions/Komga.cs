using Common.Helpers;
using Extensions.Data;

namespace Extensions.Extensions;

public sealed class Komga(Guid id, string name, string baseUrl) : ILibraryExtension<KomgaSeries, KomgaBook>
{
    public Guid Identifier { get; init; } = id;
    public string Name { get; init; } = name;
    public string BaseUrl { get; init; } = baseUrl;

    public Task<KomgaSeries[]> GetSeriesList()
    {
        throw new NotImplementedException();
    }

    public Task AddSeries(KomgaSeries series)
    {
        throw new NotImplementedException();
    }

    public Task<KomgaSeries> GetSeries()
    {
        throw new NotImplementedException();
    }

    public Task UpdateSeries(KomgaSeries series)
    {
        throw new NotImplementedException();
    }

    public Task UpdatePoster(Guid seriesId, TrangaImage poster)
    {
        throw new NotImplementedException();
    }

    public Task DeleteSeries(Guid seriesId)
    {
        throw new NotImplementedException();
    }

    public Task AddBook(Guid seriesId, KomgaBook book)
    {
        throw new NotImplementedException();
    }

    public Task<KomgaBook[]> GetBooks(Guid seriesId)
    {
        throw new NotImplementedException();
    }

    public Task UpdateBook(Guid bookId, KomgaBook book)
    {
        throw new NotImplementedException();
    }

    public Task UpdateBookCover(Guid bookId, TrangaImage cover)
    {
        throw new NotImplementedException();
    }

    public Task DeleteBook(Guid bookId)
    {
        throw new NotImplementedException();
    }
}

public sealed record KomgaSeries() : ISeries
{
    public Guid SeriesId { get; init; }
}

public sealed record KomgaBook() : IBook
{
    public Guid BookId { get; init; }
}