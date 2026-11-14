using Common.Datatypes;
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
     public Task<List<SearchResult>?> SearchMetadata(SearchQuery searchQuery, CancellationToken ct);

     /// <summary>
     /// Parses a manga page URL on this extension's own site (e.g. pasted by a user into the search) into a
     /// <see cref="SearchQuery"/> that looks up exactly that series, without any network access.
     /// </summary>
     /// <param name="url">The manga's page URL on the extension's site.</param>
     /// <returns>A query with only this extension's series-id set, or <see langword="null"/> if <paramref name="url"/> is not one of this extension's manga pages.</returns>
     public SearchQuery? ParseSearchQueryFromUrl(string url);
}
