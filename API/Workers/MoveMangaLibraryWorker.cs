using API.Schema.MangaContext;
using Microsoft.EntityFrameworkCore;

namespace API.Workers;

public class MoveMangaLibraryWorker(Manga manga, FileLibrary toLibrary, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContext<MangaContext>(dependsOn)
{
    internal readonly string MangaId = manga.Key;
    internal readonly string LibraryId = toLibrary.Key;
    protected override async Task<BaseWorker[]> DoWorkInternal()
    {
        if (await DbContext.Mangas.FirstOrDefaultAsync(m => m.Key == MangaId, CancellationTokenSource.Token) is not { } manga)
            return []; //TODO Exception?
        if (await DbContext.FileLibraries.FirstOrDefaultAsync(l => l.Key == LibraryId, CancellationTokenSource.Token) is not { } toLibrary)
            return []; //TODO Exception?
        
        await DbContext.Entry(manga).Collection(m => m.Chapters).LoadAsync(CancellationTokenSource.Token);
        await DbContext.Entry(manga).Navigation(nameof(Manga.Library)).LoadAsync(CancellationTokenSource.Token);
        
        Dictionary<Chapter, string> oldPath = manga.Chapters.ToDictionary(c => c, c => c.FullArchiveFilePath);
        manga.Library = toLibrary;

        if (await DbContext.Sync(CancellationTokenSource.Token) is { success: false })
            return [];

        return manga.Chapters.Select(c => new MoveFileOrFolderWorker(c.FullArchiveFilePath, oldPath[c])).ToArray<BaseWorker>();
    }

    public override string ToString() => $"{base.ToString()} {MangaId} {LibraryId}";
}