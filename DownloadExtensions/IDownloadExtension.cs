using DownloadExtensions.Data;
using DownloadExtensions.Helpers;

namespace DownloadExtensions;

public interface IDownloadExtension<T> where T : IDownloadExtension<T>
{
    /// <summary>
    /// The unique Extension Identifier
    /// </summary>
    public Guid Identifier { get; init; }
    
    /// <summary>
    /// The name of the Extension
    /// </summary>
    public string Name { get; init; }
    
    /// <summary>
    /// The languages supported by the extension.
    /// </summary>
    public Language[] SupportedLanguages { get; init; }
    
    /// <summary>
    /// The Url of the extension
    /// </summary>
    public string BaseUrl { get; init; }
    
    /// <summary>
    /// Returns the search results for a Manga.
    /// </summary>
    /// <param name="query">The manga to search for.</param>
    /// <param name="ct">Cancellation-token for the operation.</param>
    /// <returns>A Task representing the operation. null indicates a failure.</returns>
    public Task<MangaSearchResult<T>?> Search(SearchQuery query, CancellationToken ct);

    /// <summary>
    /// Returns the chapters of a Manga.
    /// </summary>
    /// <param name="manga"></param>
    /// <param name="ct">Cancellation-token for the operation.</param>
    /// <returns>A Task representing the operation. null indicates a failure.</returns>
    public Task<List<Chapter<T>>?> GetChapters(Manga<T> manga, CancellationToken ct);

    /// <summary>
    /// Returns the images of a chapter.
    /// </summary>
    /// <param name="chapter"></param>
    /// <param name="ct">Cancellation-token for the operation.</param>
    /// <returns>A Task representing the operation. null indicates a failure.</returns>
    public Task<List<ChapterImage<T>>?> GetChapterImages(Chapter<T> chapter, CancellationToken ct);
}