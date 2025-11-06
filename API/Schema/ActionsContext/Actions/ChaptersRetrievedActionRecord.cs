using API.Schema.ActionsContext.Actions.Generic;
using API.Schema.MangaContext;

namespace API.Schema.ActionsContext.Actions;

public sealed class ChaptersRetrievedActionRecord(Actions action, DateTime performedAt, string mangaId)
    : ActionRecord(action, performedAt), IActionWithMangaRecord
{
    public ChaptersRetrievedActionRecord(Manga manga) : this(Actions.ChaptersRetrieved, DateTime.UtcNow, manga.Key) { }

    public string MangaId { get; init; } = mangaId;
}