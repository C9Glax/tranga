using System.ComponentModel.DataAnnotations;
using API.Schema.ActionsContext.Actions.Generic;
using API.Schema.MangaContext;

namespace API.Schema.ActionsContext.Actions;

public sealed class LibraryMovedActionRecord(ActionsEnum action, DateTime performedAt, string mangaId, string fileLibraryId)
    : ActionRecord(action, performedAt), IActionWithMangaRecord
{
    public LibraryMovedActionRecord(Manga manga, FileLibrary library) : this(ActionsEnum.LibraryMoved, DateTime.UtcNow, manga.Key, library.Key) { }
    
    /// <summary>
    /// <see cref="Schema.MangaContext.FileLibrary"/> for which the cover was downloaded
    /// </summary>
    [StringLength(64)]
    public string FileLibraryId { get; init; } = fileLibraryId;

    public string MangaId { get; init; } = mangaId;
}