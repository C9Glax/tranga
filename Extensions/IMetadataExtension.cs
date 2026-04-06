using Extensions.Data;

namespace Extensions;

public interface IMetadataExtension : IExtension
{
     /// <summary>
     /// Searches the Metadata-Provider using the <paramref name="searchQuery"/>
     /// </summary>
     /// <param name="searchQuery">Query to use for searching Manga</param>
     /// <param name="ct">The Cancellation Token for the Task</param>
     /// <returns>A Task representing the long running operation.</returns>
     public Task<List<SearchResult>?> SearchMetadata(Common.Datatypes.SearchQuery searchQuery, CancellationToken ct);
}