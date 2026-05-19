using Common.Helpers;
using Extensions.Data;

namespace Extensions;

public interface ILibraryExtension<TSeries, TBook, TIdentifier> where TSeries : ISeries<TIdentifier> where TBook : IBook<TIdentifier> where TIdentifier : IIdentifier<TIdentifier>
{
    public Task<TIdentifier> CreateTrangaLibrary(CancellationToken ct, string? rootDir = null);
    
    public Task<TSeries[]> GetSeriesList(CancellationToken ct);

    public Task UpdateSeriesMetadata(TSeries series, CancellationToken ct);

    public Task UpdateSeriesPoster(TIdentifier seriesId, TrangaImage poster, CancellationToken ct);

    public Task ScanLibrary(TIdentifier libraryId, CancellationToken ct);
}