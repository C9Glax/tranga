using Common.Services.Events;
using Common.Services.Events.Events;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;
using Services.Manga.Database.Helpers;

namespace Services.Manga.Features.Chapters;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class DeleteChapterEndpoint
{
    /// <summary>
    /// Delete Chapter by ID
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="eventPublisher"></param>
    /// <param name="chapterId">ID of Chapter</param>
    /// <param name="ct"></param>
    /// <remarks>
    /// Deletes the Chapter's downloaded file (if any) from disk along with its database entries. Linked library
    /// services (e.g. Komga) are notified to re-scan, so the deletion is reflected there as well.
    /// </remarks>
    /// <response code="200">Chapter has been deleted</response>
    /// <response code="404">Chapter with requested ID does not exist</response>
    public static async Task<Results<Ok, NotFound>> Handle(MangaContext mangaContext, [FromServices] EventPublisher eventPublisher,
        [FromRoute] Guid chapterId, CancellationToken ct)
    {
        DbChapter? chapter = await mangaContext.Chapters
            .Include(c => c.DownloadLinks)!
            .ThenInclude(l => l.File)
            .SingleOrDefaultAsync(c => c.ChapterId == chapterId, ct);

        if (chapter is null)
            return TypedResults.NotFound();

        foreach (DbChapterDownloadLink link in chapter.DownloadLinks ?? [])
        {
            if (link.File is not { } file)
                continue;

            file.DeleteFile();
            mangaContext.Files.Remove(file);
        }

        Guid mangaId = chapter.MangaId;
        mangaContext.Chapters.Remove(chapter);
        await mangaContext.SaveChangesAsync(ct);

        await eventPublisher.PublishAsync(new ChapterDeletedEvent(mangaId), ct);

        return TypedResults.Ok();
    }
}
