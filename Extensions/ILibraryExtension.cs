using Common.Helpers;
using Extensions.Data;

namespace Extensions;

public interface ILibraryExtension<TSeries, TBook> : IExtension where TSeries : ISeries where TBook : IBook
{
    public Task<TSeries[]> GetSeriesList();

    public Task AddSeries(TSeries series);
    
    public Task<TSeries> GetSeries();

    public Task UpdateSeries(TSeries series);

    public Task UpdatePoster(Guid seriesId, TrangaImage poster);

    public Task DeleteSeries(Guid seriesId);

    public Task AddBook(Guid seriesId, TBook book);

    public Task<TBook[]> GetBooks(Guid seriesId);

    public Task UpdateBook(Guid bookId, TBook book);
    
    public Task UpdateBookCover(Guid bookId, TrangaImage cover);

    public Task DeleteBook(Guid bookId);
}