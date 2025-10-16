using System.ComponentModel.DataAnnotations;
using API.Schema.ActionsContext.Actions.Generic;
using API.Schema.MangaContext;

namespace API.Schema.ActionsContext.Actions;

public sealed class LibraryMovedActionRecord(string action, DateTime performedAt, string mangaId, string fileLibraryId) : ActionWithMangaRecord(action, performedAt, mangaId)
{
    public LibraryMovedActionRecord(Manga manga, FileLibrary library) : this(LibraryMovedAction, DateTime.UtcNow, manga.Key, library.Key) { }
    
    /// <summary>
    /// <see cref="Schema.MangaContext.FileLibrary"/> for which the cover was downloaded
    /// </summary>
    [StringLength(64)]
    public string FileLibraryId { get; init; } = fileLibraryId;
    
    public const string LibraryMovedAction = "Manga.LibraryMoved";
}