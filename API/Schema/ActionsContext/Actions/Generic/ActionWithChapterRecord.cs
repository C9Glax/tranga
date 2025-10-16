using System.ComponentModel.DataAnnotations;
using API.Schema.MangaContext;

namespace API.Schema.ActionsContext.Actions.Generic;

public abstract class ActionWithChapterRecord(ActionsEnum action, DateTime performedAt, string chapterId) : ActionRecord(action, performedAt)
{
    protected ActionWithChapterRecord(ActionsEnum action, DateTime performedAt, Chapter chapter) : this(action, performedAt, chapter.Key) { }
    
    /// <summary>
    /// <see cref="Schema.MangaContext.Manga"/> for which the cover was downloaded
    /// </summary>
    [StringLength(64)]
    public string ChapterId { get; init; } = chapterId;
}