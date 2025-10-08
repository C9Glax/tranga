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
        // Get Manga (with and Library)
        if (await DbContext.Mangas
                .Include(m => m.Library)
                .Include(m => m.Chapters).ThenInclude(ch => ch.ParentManga).ThenInclude(m => m.Library) //kind of redundant, but better be safe
                .FirstOrDefaultAsync(m => m.Key == MangaId, CancellationToken) is not { } manga)
        {
            Log.Error("Could not find Manga.");
            return [];
        }
        
        // Get new Library
        if (await DbContext.FileLibraries.FirstOrDefaultAsync(l => l.Key == LibraryId, CancellationToken) is not { } toLibrary)
        {
            Log.Error("Could not find Library.");
            return [];
        }
        
        // Save old Path (to later move chapters)
        Dictionary<string, string> oldPath = manga.Chapters.Where(c => c.FileName != null).ToDictionary(c => c.Key, c => c.FullArchiveFilePath)!;
        // Set new Path
        manga.Library = toLibrary;
        
        if (await DbContext.Sync(CancellationToken, GetType(), System.Reflection.MethodBase.GetCurrentMethod()?.Name) is { success: false })
            return [];

        // Create Jobs to move chapters from old to new Path
        return oldPath.Select(kv => new MoveFileOrFolderWorker(manga.Chapters.First(ch => ch.Key == kv.Key).FullArchiveFilePath!, kv.Value)).ToArray<BaseWorker>();
    }

    public override string ToString() => $"{base.ToString()} {MangaId} {LibraryId}";
}