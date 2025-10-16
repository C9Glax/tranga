using API.Schema.ActionsContext.Actions.Generic;
using API.Schema.MangaContext;

namespace API.Schema.ActionsContext.Actions;

public sealed class ChaptersRetrievedActionRecord(ActionsEnum action, DateTime performedAt, string mangaId)
    : ActionRecord(action, performedAt), IActionWithMangaRecord
{
    public ChaptersRetrievedActionRecord(Manga manga) : this(ActionsEnum.ChaptersRetrieved, DateTime.UtcNow, manga.Key) { }

    public string MangaId { get; init; } = mangaId;
}