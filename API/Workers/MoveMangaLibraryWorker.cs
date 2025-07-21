using API.Schema.MangaContext;

namespace API.Workers;

public class MoveMangaLibraryWorker(Manga manga, FileLibrary toLibrary, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContext<MangaContext>(dependsOn)
{
    internal readonly string MangaId = manga.Key;
    internal readonly string LibraryId = toLibrary.Key;
    protected override BaseWorker[] DoWorkInternal()
    {
        if (DbContext.Mangas.Find(MangaId) is not { } manga)
            return []; //TODO Exception?
        if (DbContext.FileLibraries.Find(LibraryId) is not { } toLibrary)
            return []; //TODO Exception?
        
        DbContext.Entry(manga).Collection(m => m.Chapters).Load();
        DbContext.Entry(manga).Navigation(nameof(Manga.Library)).Load();
        
        Dictionary<Chapter, string> oldPath = manga.Chapters.ToDictionary(c => c, c => c.FullArchiveFilePath);
        manga.Library = toLibrary;

        if (DbContext.Sync() is { success: false })
            return [];

        return manga.Chapters.Select(c => new MoveFileOrFolderWorker(c.FullArchiveFilePath, oldPath[c])).ToArray<BaseWorker>();
    }

    public override string ToString() => $"{base.ToString()} {MangaId} {LibraryId}";
}