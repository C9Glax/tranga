using System.ComponentModel.DataAnnotations;
using API.Schema.MangaContext;

namespace API.Schema.ActionsContext.Actions;

public sealed class ChapterDownloadedActionRecord(ActionsEnum action, DateTime performedAt, string chapterId) : ActionRecord(action, performedAt)
{
    public ChapterDownloadedActionRecord(Chapter chapter) : this(ActionsEnum.ChapterDownloaded, DateTime.UtcNow, chapter.Key) { }

    /// <summary>
    /// Chapter that was downloaded
    /// </summary>
    [StringLength(64)]
    public string ChapterId { get; init; } = chapterId;
}