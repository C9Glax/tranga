using API.Schema.ActionsContext.Actions.Generic;
using API.Schema.MangaContext;

namespace API.Schema.ActionsContext.Actions;

public sealed class ChapterDownloadedActionRecord(ActionsEnum action, DateTime performedAt, string chapterId) : ActionWithChapterRecord(action, performedAt, chapterId)
{
    public ChapterDownloadedActionRecord(Chapter chapter) : this(ActionsEnum.ChapterDownloaded, DateTime.UtcNow, chapter.Key) { }
}