using Common.Helpers;
using Common.Tests;
using Services.Manga.Database;
using Services.Manga.Database.Helpers;
using Services.Manga.Helpers;

namespace Services.Manga.Tests.Helpers;

public class DownloadLinkCoverHelperTests : TrangaTest, IDisposable
{
    private readonly string _sourceDirectory = Path.Combine(Path.GetTempPath(), "TrangaTests", Guid.NewGuid().ToString("N"));
    private readonly List<DbFile> _created = [];

    public void Dispose()
    {
        foreach (DbFile file in _created)
            file.DeleteFile();
        if (Directory.Exists(_sourceDirectory))
            Directory.Delete(_sourceDirectory, recursive: true);
    }

    private async Task<DbFile> SeedFile(MangaContext context, byte[] content)
    {
        DbFile file = new() { FileId = Guid.NewGuid(), Path = _sourceDirectory, Name = "cover.jpg", MimeType = "image/jpeg" };
        await file.SaveFile(new MemoryStream(content), ct);
        await context.AddAsync(file, ct);
        await context.SaveChangesAsync(ct);
        return file;
    }

    [Fact]
    public async Task SaveCover_StoresTheImageAsJpegAndSetsItOnTheLink()
    {
        await using MangaContext context = MangaContextFactory.Create();
        DbDownloadLink link = TestDataBuilder.NewDownloadLink();

        bool saved = await context.SaveCover(link, FakeDownloadExtension.Jpeg(), ct);

        Assert.True(saved);
        Assert.NotNull(link.Cover);
        _created.Add(link.Cover);
        Assert.Equal(link.Cover.FileId, link.CoverId);
        Assert.Equal("image/jpeg", link.Cover.MimeType);
        Assert.True(File.Exists(link.Cover.FullPath));
    }

    [Fact]
    public async Task SaveCover_IgnoresDataThatIsNoImage()
    {
        await using MangaContext context = MangaContextFactory.Create();
        DbDownloadLink link = TestDataBuilder.NewDownloadLink();
        TrangaImage notAnImage = new();
        await notAnImage.WriteAsync("definitely not an image"u8.ToArray(), ct);

        bool saved = await context.SaveCover(link, notAnImage, ct);

        Assert.False(saved);
        Assert.Null(link.CoverId);
        Assert.Null(link.Cover);
    }

    [Fact]
    public async Task CopyCover_StoresAnIdenticalCopyAsAFileOfItsOwn()
    {
        await using MangaContext context = MangaContextFactory.Create();
        byte[] content = FakeDownloadExtension.Jpeg().ToArray();
        DbFile source = await SeedFile(context, content);
        DbDownloadLink link = TestDataBuilder.NewDownloadLink();

        bool copied = await context.CopyCover(link, source.FileId, ct);

        Assert.True(copied);
        Assert.NotNull(link.Cover);
        _created.Add(link.Cover);
        Assert.NotEqual(source.FileId, link.CoverId);
        Assert.Equal(link.Cover.FileId, link.CoverId);
        Assert.Equal(source.MimeType, link.Cover.MimeType);
        Assert.NotEqual(source.FullPath, link.Cover.FullPath);
        Assert.Equal(content, await File.ReadAllBytesAsync(link.Cover.FullPath, ct));
        // the original is untouched
        Assert.Equal(content, await File.ReadAllBytesAsync(source.FullPath, ct));
    }

    [Fact]
    public async Task CopyCover_DoesNothingWithoutAReadableSourceCover()
    {
        await using MangaContext context = MangaContextFactory.Create();
        DbDownloadLink link = TestDataBuilder.NewDownloadLink();

        Assert.False(await context.CopyCover(link, null, ct));
        Assert.False(await context.CopyCover(link, Guid.NewGuid(), ct));

        // a file that is in the database but no longer on disk
        DbFile source = await SeedFile(context, [1, 2, 3]);
        source.DeleteFile();
        Assert.False(await context.CopyCover(link, source.FileId, ct));
        Assert.Null(link.CoverId);
        Assert.Null(link.Cover);
    }
}
