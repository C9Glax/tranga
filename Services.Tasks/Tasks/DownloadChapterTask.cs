using Extensions;
using Extensions.Data;
using Services.Manga.Database;
using Services.Manga.Helpers;
using Services.Tasks.TaskTypes;

namespace Services.Tasks.Tasks;

/// <summary>
/// Downloads a <see cref="DbChapter"/> using the <see cref="DbChapterDownloadLink"/> with the highest Priority.
/// </summary>
/// <param name="chapter"></param>
internal sealed class DownloadChapterTask(DbChapter chapter) : RunOnceTask(Guid.Parse("87d2b155-5723-4483-a2f9-c15292a14f44"))
{
    internal Guid ChapterId { get; init; } = chapter.ChapterId;
    
    private MangaContext _ctx = null!;
    
    private protected override async Task RunAsync(IServiceScope scope, ILogger logger, CancellationToken stoppingToken)
    {
        await _ctx.Entry(chapter).Collection(c => c.DownloadLinks!).LoadAsync(stoppingToken);
        if (chapter.DownloadLinks!.FirstOrDefault(d => d.FileId != null) is { } file)
        {
            logger.LogDebug("Chapter is already downloaded. File {file.FileId}", file.FileId);
            return;
        }
        if (chapter.DownloadLinks!.MinBy(d => d.Priority) is not { } link)
            return;
        logger.LogDebug("Got {link.DownloadExtension} {link.Identifier}.", link.DownloadExtension, link.Identifier);
        if (DownloadExtensionsCollection.GetExtension(link.DownloadExtension) is not { } extension)
            return;

        List<ChapterImage>? images = await extension.GetChapterImages(link.ToChapterInfo(), stoppingToken);
        
        // TODO DbFile and saving
    }

    private protected override void RefreshScope(IServiceScope scope)
    {
        _ctx = scope.ServiceProvider.GetRequiredService<MangaContext>();
    }
    
    public override string ToString() => $"{base.ToString()} - Manga {chapter.MangaId} - Chapter {ChapterId} {chapter.Volume} {chapter.Number}";
}