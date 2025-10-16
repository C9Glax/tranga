using API.Schema.ActionsContext.Actions.Generic;
using API.Schema.MangaContext;

namespace API.Schema.ActionsContext.Actions;

public sealed class ChapterDownloadedActionRecord(ActionsEnum action, DateTime performedAt, string mangaId, string chapterId) : ActionRecord(action, performedAt), IActionWithChapterRecord, IActionWithMangaRecord
{
    public ChapterDownloadedActionRecord(Manga manga, Chapter chapter) : this(ActionsEnum.ChapterDownloaded, DateTime.UtcNow, manga.Key, chapter.Key) { }
    public string ChapterId { get; init; } = chapterId;
    public string MangaId { get; init; } = mangaId;
}