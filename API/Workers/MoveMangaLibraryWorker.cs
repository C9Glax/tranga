using API.Schema.MangaContext;

namespace API.Workers;

public class MoveMangaLibraryWorker(Manga manga, FileLibrary toLibrary, IServiceScope scope, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContext<MangaContext>(dependsOn)
{
    protected override BaseWorker[] DoWorkInternal()
    {
        Dictionary<Chapter, string> oldPath = manga.Chapters.ToDictionary(c => c, c => c.FullArchiveFilePath);
        manga.Library = toLibrary;

        if (DbContext.Sync().Result is { success: false })
            return [];

        return manga.Chapters.Select(c => new MoveFileOrFolderWorker(c.FullArchiveFilePath, oldPath[c])).ToArray<BaseWorker>();
    }

    public override string ToString() => $"{base.ToString()} {manga} {toLibrary}";
}