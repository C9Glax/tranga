using Common.Datatypes;
using DownloadExtensions.Data;

namespace DownloadExtensions;

public interface IDownloadExtension
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
    public Task<List<MangaInfo>?> Search(SearchQuery query, CancellationToken ct);

    /// <summary>
    /// Returns the chapters of a Manga.
    /// </summary>
    /// <param name="mangaInfo"></param>
    /// <param name="ct">Cancellation-token for the operation.</param>
    /// <returns>A Task representing the operation. null indicates a failure.</returns>
    public Task<List<ChapterInfo>?> GetChapters(MangaInfo mangaInfo, CancellationToken ct);

    /// <summary>
    /// Returns the images of a chapter.
    /// </summary>
    /// <param name="chapterInfo"></param>
    /// <param name="ct">Cancellation-token for the operation.</param>
    /// <returns>A Task representing the operation. null indicates a failure.</returns>
    public Task<List<ChapterImage>?> GetChapterImages(ChapterInfo chapterInfo, CancellationToken ct);
}