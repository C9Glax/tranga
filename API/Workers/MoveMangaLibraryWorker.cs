using API.Schema.MangaContext;
using Microsoft.EntityFrameworkCore;

namespace API.Workers;

public class MoveMangaLibraryWorker(Manga manga, FileLibrary toLibrary, IServiceScope scope, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContext<MangaContext>(scope, dependsOn)
{
    protected override BaseWorker[] DoWorkInternal()
    {
        Dictionary<Chapter, string> oldPath = manga.Chapters.ToDictionary(c => c, c => c.FullArchiveFilePath);
        manga.Library = toLibrary;
        try
        {
            DbContext.SaveChanges();
        }
        catch (DbUpdateException e)
        {
            Log.Error(e);
            return [];
        }

        return manga.Chapters.Select(c => new MoveFileOrFolderWorker(c.FullArchiveFilePath, oldPath[c])).ToArray<BaseWorker>();
    }
}