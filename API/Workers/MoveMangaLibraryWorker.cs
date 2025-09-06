using API.Schema.MangaContext;
using Microsoft.EntityFrameworkCore;

namespace API.Workers;

/// <summary>
/// Moves a Manga to a different Library
/// </summary>
public class MoveMangaLibraryWorker(Manga manga, FileLibrary toLibrary, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContext<MangaContext>(dependsOn)
{
    internal readonly string MangaId = manga.Key;
    internal readonly string LibraryId = toLibrary.Key;
    protected override async Task<BaseWorker[]> DoWorkInternal()
    {
        Log.Debug("Moving Manga...");
        // Get Manga (with Chapters and Library)
        if (await DbContext.Mangas
                .Include(m => m.Chapters)
                .Include(m => m.Library)
                .FirstOrDefaultAsync(m => m.Key == MangaId, CancellationToken) is not { } manga)
        {
            Log.Error("Could not find Manga.");
            return []; //TODO Exception?
        }
        // Get new Library
        if (await DbContext.FileLibraries.FirstOrDefaultAsync(l => l.Key == LibraryId, CancellationToken) is not { } toLibrary)
        {
            Log.Error("Could not find Library.");
            return []; //TODO Exception?
        }
        
        // Save old Path (to later move chapters)
        Dictionary<Chapter, string> oldPath = manga.Chapters.ToDictionary(c => c, c => c.FullArchiveFilePath);
        // Set new Path
        manga.Library = toLibrary;
        
        if (await DbContext.Sync(CancellationToken) is { success: false })
            return [];

        // Create Jobs to move chapters from old to new Path
        return manga.Chapters.Select(c => new MoveFileOrFolderWorker(c.FullArchiveFilePath, oldPath[c])).ToArray<BaseWorker>();
    }

    public override string ToString() => $"{base.ToString()} {MangaId} {LibraryId}";
}