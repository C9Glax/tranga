using System.ComponentModel.DataAnnotations;
using API.Schema.MangaContext;

namespace API.Schema.ActionsContext.Actions.Generic;

public abstract class ActionWithMangaRecord(ActionsEnum action, DateTime performedAt, string mangaId) : ActionRecord(action, performedAt)
{
    protected ActionWithMangaRecord(ActionsEnum action, DateTime performedAt, Manga manga) : this(action, performedAt, manga.Key) { }
    
    /// <summary>
    /// <see cref="Schema.MangaContext.Manga"/> for which the cover was downloaded
    /// </summary>
    [StringLength(64)]
    public string MangaId { get; init; } = mangaId;
}