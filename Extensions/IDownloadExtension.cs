using Common.Datatypes;
using Common.Helpers;
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
    /// Parses the extension-specific series <see cref="MangaInfo.Identifier"/> out of a manga page URL on this
    /// extension's own site (e.g. as pasted by a user adding a Download-Link manually), without any network access.
    /// </summary>
    /// <param name="url">The manga's page URL on the extension's site.</param>
    /// <returns>The parsed identifier, or <see langword="null"/> if <paramref name="url"/> does not match this extension's expected URL shape.</returns>
    public string? ParseIdentifierFromUrl(string url);

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
    protected Task<List<ChapterImage>?> FetchChapterImages(ChapterInfo chapterInfo, CancellationToken ct);
    
    /// <summary>
    /// Returns the images of a chapter.
    /// </summary>
    /// <param name="chapterInfo"></param>
    /// <param name="ct">Cancellation-token for the operation.</param>
    /// <returns>A Task representing the operation. null indicates a failure.</returns>
    public async Task<List<ChapterImage>?> GetChapterImages(ChapterInfo chapterInfo, CancellationToken ct)
    {
        if (await this.FetchChapterImages(chapterInfo, ct) is not { } images)
            return null;

        List<Task> tasks = images.Select(i => i.image.Process(ct)).ToList();
        await Task.WhenAll(tasks);
        if (tasks.Any(t => t is { IsCompletedSuccessfully: false }))
            return null;

        return images;
    }
}