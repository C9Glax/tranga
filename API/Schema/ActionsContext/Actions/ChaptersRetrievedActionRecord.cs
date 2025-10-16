using API.Schema.ActionsContext.Actions.Generic;
using API.Schema.MangaContext;

namespace API.Schema.ActionsContext.Actions;

public sealed class ChaptersRetrievedActionRecord(ActionsEnum action, DateTime performedAt, string mangaId)
    : ActionWithMangaRecord(action, performedAt, mangaId)
{
    public ChaptersRetrievedActionRecord(Manga manga) : this(ActionsEnum.ChaptersRetrieved, DateTime.UtcNow, manga.Key) { }

    public const string ChaptersRetrievedAction = "Manga.ChaptersRetrieved";
}