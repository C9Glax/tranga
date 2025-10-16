using System.ComponentModel.DataAnnotations;
using API.Schema.ActionsContext.Actions.Generic;
using API.Schema.MangaContext;

namespace API.Schema.ActionsContext.Actions;

public sealed class CoverDownloadedActionRecord(string action, DateTime performedAt, string mangaId, string filename)
    : ActionWithMangaRecord(action, performedAt, mangaId)
{
    public CoverDownloadedActionRecord(Manga manga, string filename) : this(CoverDownloadedAction, DateTime.UtcNow, manga.Key, filename) { }

    /// <summary>
    /// Filename on disk
    /// </summary>
    [StringLength(1024)]
    public string Filename { get; init; } = filename;

    public const string CoverDownloadedAction = "Manga.CoverDownloaded";
}