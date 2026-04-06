using Common.Datatypes;
using Extensions.Data;

namespace Extensions;

public interface IDownloadExtension : IExtension
{
    /// <summary>
    /// The languages supported by the extension.
    /// </summary>
    public Language[] SupportedLanguages { get; init; }
    
    /// <summary>
    /// Returns the search results for a Manga.
    /// </summary>
    /// <param name="query">The manga to search for.</param>
    /// <param name="ct">Cancellation-token for the operation.</param>
    /// <returns>A Task representing the operation. null indicates a failure.</returns>
    public Task<List<MangaInfo>?> SearchDownload(SearchQuery query, CancellationToken ct);

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