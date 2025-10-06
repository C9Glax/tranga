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
                .FirstOrDefaultAsync(m => m.Key == MangaId, CancellationToken) is not { } manga)
        {
            Log.Error("Could not find Manga.");
            return [];
        }

        if (await DbContext.Chapters
                .Include(ch => ch.ParentManga).ThenInclude(m => m.Library)
                .Where(ch => ch.ParentMangaId == MangaId)
                .ToListAsync(CancellationToken) is not { } chapters)
        {
            Log.Error("Could not find chapters.");
            return [];
        }
        
        // Get new Library
        if (await DbContext.FileLibraries.FirstOrDefaultAsync(l => l.Key == LibraryId, CancellationToken) is not { } toLibrary)
        {
            Log.Error("Could not find Library.");
            return [];
        }
        
        // Save old Path (to later move chapters)
        Dictionary<string, string> oldPath = manga.Chapters.ToDictionary(c => c.Key, c => c.FullArchiveFilePath).Where(kv => kv.Value is not null).ToDictionary(x => x.Key, x => x.Value)!;
        // Set new Path
        manga.Library = toLibrary;
        
        if (await DbContext.Sync(CancellationToken, GetType(), System.Reflection.MethodBase.GetCurrentMethod()?.Name) is { success: false })
            return [];

        // Create Jobs to move chapters from old to new Path
        return manga.Chapters.Select(c => new MoveFileOrFolderWorker(c.FullArchiveFilePath, oldPath[c.Key])).ToArray<BaseWorker>();
    }

    public override string ToString() => $"{base.ToString()} {MangaId} {LibraryId}";
}