using Common.Helpers;
using Extensions.Data;

namespace Extensions;

public interface ILibraryExtension<TSeries, TBook, in TIdentifier> where TSeries : ISeries<TIdentifier> where TBook : IBook<TIdentifier> where TIdentifier : IIdentifier
{
    public Task<TSeries[]> GetSeriesList(CancellationToken ct);

    public Task AddSeries(TSeries series, CancellationToken ct);
    
    public Task<TSeries> GetSeries(TIdentifier id, CancellationToken ct);

    public Task UpdateSeries(TSeries series, CancellationToken ct);

    public Task UpdatePoster(TIdentifier seriesId, TrangaImage poster, CancellationToken ct);

    public Task DeleteSeries(TIdentifier seriesId, CancellationToken ct);

    public Task AddBook(TIdentifier seriesId, TBook book, CancellationToken ct);

    public Task<TBook[]> GetBooks(TIdentifier seriesId, CancellationToken ct);

    public Task UpdateBook(TIdentifier bookId, TBook book, CancellationToken ct);
    
    public Task UpdateBookCover(TIdentifier bookId, TrangaImage cover, CancellationToken ct);

    public Task DeleteBook(TIdentifier bookId, CancellationToken ct);
}