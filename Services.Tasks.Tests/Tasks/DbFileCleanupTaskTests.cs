using Common.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Services.Manga.Database;
using Services.Tasks.Tasks;
using Services.Tasks.Tests.Helpers;

namespace Services.Tasks.Tests.Tasks;

public class DbFileCleanupTaskTests : TrangaTest
{
    private static DbFile NewFile() => new()
        { FileId = Guid.CreateVersion7(), Path = "/tmp", Name = $"{Guid.NewGuid():N}.bin", MimeType = "application/octet-stream" };

    [Fact]
    public async Task RunAsync_DeletesOrphanedFiles_KeepsReferencedFiles()
    {
        IServiceProvider services = BuildServices();
        Guid mangaId = Guid.CreateVersion7();
        DbFile orphan = NewFile(), chapterFile = NewFile(), metadataCover = NewFile(), linkCover = NewFile();

        using (IServiceScope seedScope = services.CreateScope())
        {
            MangaContext seedCtx = seedScope.ServiceProvider.GetRequiredService<MangaContext>();
            await seedCtx.Mangas.AddAsync(new DbManga { MangaId = mangaId, Monitored = true }, ct);
            DbChapter chapter = new() { MangaId = mangaId, Number = "1" };
            await seedCtx.Chapters.AddAsync(chapter, ct);
            await seedCtx.Files.AddRangeAsync([orphan, chapterFile, metadataCover, linkCover], ct);
            await seedCtx.SaveChangesAsync(ct);

            await seedCtx.ChapterDownloadLinks.AddAsync(new DbChapterDownloadLink
            {
                ChapterId = chapter.ChapterId, DownloadExtension = Guid.NewGuid(), Identifier = "c1", Priority = 0,
                FileId = chapterFile.FileId
            }, ct);
            await seedCtx.MetadataEntries.AddAsync(new DbMetadata
                { MetadataExtension = Guid.NewGuid(), Identifier = "m1", Series = "S", CoverId = metadataCover.FileId }, ct);
            await seedCtx.DownloadLinks.AddAsync(new DbDownloadLink
                { DownloadExtension = Guid.NewGuid(), Identifier = "d1", Series = "S", CoverId = linkCover.FileId }, ct);
            await seedCtx.SaveChangesAsync(ct);
        }

        using (IServiceScope scope = services.CreateScope())
            await new DbFileCleanupTask().ExecuteAsync(scope, NoOpLogger.Instance, ct);

        using (IServiceScope verifyScope = services.CreateScope())
        {
            MangaContext ctx = verifyScope.ServiceProvider.GetRequiredService<MangaContext>();
            Guid[] remaining = await ctx.Files.Select(f => f.FileId).ToArrayAsync(ct);
            Assert.DoesNotContain(orphan.FileId, remaining);
            Assert.Contains(chapterFile.FileId, remaining);
            Assert.Contains(metadataCover.FileId, remaining);
            Assert.Contains(linkCover.FileId, remaining);
        }
    }

    private static IServiceProvider BuildServices()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), "TrangaTests", "Services.Tasks.Tests.MangaContext", $"{Guid.NewGuid():N}.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        ServiceCollection services = new();
        services.AddDbContext<MangaContext>(options => options.UseSqlite($"Data Source={dbPath}"));
        ServiceProvider provider = services.BuildServiceProvider();

        using IServiceScope scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<MangaContext>().Database.EnsureCreated();

        return provider;
    }
}
