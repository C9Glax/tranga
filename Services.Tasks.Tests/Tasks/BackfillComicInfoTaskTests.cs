using System.IO.Compression;
using Common.Datatypes;
using Common.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Services.Manga.Database;
using Services.Tasks.Tasks;
using Services.Tasks.Tests.Helpers;

namespace Services.Tasks.Tests.Tasks;

public class BackfillComicInfoTaskTests : TrangaTest
{
    [Fact]
    public async Task RunAsync_AddsComicInfoXml_WhenArchiveIsMissingOne()
    {
        IServiceProvider services = BuildServices();
        (DbFile dbFile, string _) = await SeedDownloadedChapter(services, "Series Name", volume: "1", number: "4", title: "Title", withComicInfo: false);

        using IServiceScope scope = services.CreateScope();
        BackfillComicInfoTask task = new();
        await task.ExecuteAsync(scope, NoOpLogger.Instance, ct);

        ConcreteComicInfo comicInfo = await ReadComicInfo(dbFile);
        Assert.Equal("Series Name", comicInfo.Series);
        Assert.Equal("Title", comicInfo.Title);
        Assert.Equal("4", comicInfo.Number);
        Assert.Equal(1, comicInfo.Volume);
    }

    [Fact]
    public async Task RunAsync_DoesNotTouchArchive_WhenComicInfoXmlAlreadyPresent()
    {
        IServiceProvider services = BuildServices();
        (DbFile dbFile, string originalContentHash) = await SeedDownloadedChapter(services, "Series Name", volume: "1", number: "4", title: "Title", withComicInfo: true);

        using IServiceScope scope = services.CreateScope();
        BackfillComicInfoTask task = new();
        await task.ExecuteAsync(scope, NoOpLogger.Instance, ct);

        byte[] bytes = await File.ReadAllBytesAsync(Path.Join(dbFile.Path, dbFile.Name), ct);
        Assert.Equal(originalContentHash, Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(bytes)));
    }

    private async Task<(DbFile File, string OriginalContentHash)> SeedDownloadedChapter(
        IServiceProvider services, string series, string? volume, string number, string? title, bool withComicInfo)
    {
        using IServiceScope seedScope = services.CreateScope();
        MangaContext ctx = seedScope.ServiceProvider.GetRequiredService<MangaContext>();

        DbManga manga = new() { Monitored = true };
        DbMetadata metadata = new() { MetadataExtension = Guid.CreateVersion7(), Identifier = "id", Series = series };
        await ctx.Mangas.AddAsync(manga, ct);
        await ctx.SaveChangesAsync(ct);
        await ctx.MangaMetadataEntries.AddAsync(new DbMangaMetadataEntries
        {
            MangaId = manga.MangaId,
            MetadataId = metadata.MetadataId,
            Chosen = true,
            Manga = manga,
            Metadata = metadata,
        }, ct);

        DbChapter chapter = new() { MangaId = manga.MangaId, Volume = volume, Number = number, Title = title };
        await ctx.Chapters.AddAsync(chapter, ct);

        string directory = Path.Combine(Path.GetTempPath(), "TrangaTests", "Services.Tasks.Tests.BackfillComicInfo", $"{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        DbFile dbFile = new() { Path = directory, Name = "chapter.cbz", MimeType = "application/zip" };
        await ctx.Files.AddAsync(dbFile, ct);

        await ctx.ChapterDownloadLinks.AddAsync(new DbChapterDownloadLink
        {
            ChapterId = chapter.ChapterId,
            DownloadExtension = Guid.CreateVersion7(),
            Identifier = "id",
            Priority = 0,
            FileId = dbFile.FileId,
        }, ct);

        await ctx.SaveChangesAsync(ct);

        await WriteArchive(dbFile, withComicInfo, series, number, volume, title);
        byte[] bytes = await File.ReadAllBytesAsync(Path.Join(dbFile.Path, dbFile.Name));
        string hash = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(bytes));

        return (dbFile, hash);
    }

    private static async Task WriteArchive(DbFile dbFile, bool withComicInfo, string series, string number, string? volume, string? title)
    {
        await using FileStream fs = new(Path.Join(dbFile.Path, dbFile.Name), FileMode.Create, FileAccess.Write);
        using ZipArchive archive = new(fs, ZipArchiveMode.Create);
        ZipArchiveEntry image = archive.CreateEntry("0.jpg");
        await using (Stream imageStream = image.Open())
            await imageStream.WriteAsync("not a real image"u8.ToArray());

        if (!withComicInfo)
            return;

        ConcreteComicInfo comicInfo = new()
        {
            Series = series,
            Title = title ?? string.Empty,
            Number = number,
            Volume = int.TryParse(volume, out int v) ? v : -1,
        };
        ZipArchiveEntry comicInfoEntry = archive.CreateEntry("ComicInfo.xml");
        await using Stream comicInfoStream = comicInfoEntry.Open();
        new System.Xml.Serialization.XmlSerializer(typeof(ConcreteComicInfo), new System.Xml.Serialization.XmlRootAttribute("ComicInfo"))
            .Serialize(comicInfoStream, comicInfo);
    }

    private static async Task<ConcreteComicInfo> ReadComicInfo(DbFile dbFile)
    {
        await using FileStream fs = new(Path.Join(dbFile.Path, dbFile.Name), FileMode.Open, FileAccess.Read);
        using ZipArchive archive = new(fs, ZipArchiveMode.Read);
        ZipArchiveEntry entry = archive.GetEntry("ComicInfo.xml") ?? throw new InvalidOperationException("ComicInfo.xml entry not found.");
        await using Stream entryStream = entry.Open();
        System.Xml.Serialization.XmlSerializer serializer = new(typeof(ConcreteComicInfo), new System.Xml.Serialization.XmlRootAttribute("ComicInfo"));
        return (ConcreteComicInfo)serializer.Deserialize(entryStream)!;
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
