using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Services.Manga.Database;
using Services.Manga.Database.Helpers;
using Services.Manga.Features.File;
using Services.Manga.Tests.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Services.Manga.Tests.Features.File;

public class GetFileEndpointTests : TrangaTest, IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "TrangaTests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public async Task GetFile_ReturnsFileById()
    {
        await using MangaContext context = MangaContextFactory.Create();
        byte[] content = "file-bytes"u8.ToArray();
        DbFile file = new() { FileId = Guid.NewGuid(), Path = _directory, Name = "file.bin", MimeType = "application/octet-stream" };
        await file.SaveFile(new MemoryStream(content), ct);
        await context.AddAsync(file, ct);
        await context.SaveChangesAsync(ct);

        Results<FileStreamHttpResult, NotFound, InternalServerError> result = await GetFileEndpoint.Handle(context, file.FileId, ct);

        FileStreamHttpResult fileResult = Assert.IsType<FileStreamHttpResult>(result.Result);
        Assert.Equal("application/octet-stream", fileResult.ContentType);
        using MemoryStream buffer = new();
        await fileResult.FileStream.CopyToAsync(buffer, ct);
        Assert.Equal(content, buffer.ToArray());
    }

    [Fact]
    public async Task GetFile_ResizesImageWhenDimensionsGiven()
    {
        await using MangaContext context = MangaContextFactory.Create();
        using Image<Rgba32> source = new(40, 20);
        MemoryStream sourceStream = new();
        source.SaveAsJpeg(sourceStream);
        DbFile file = new() { FileId = Guid.NewGuid(), Path = _directory, Name = "cover.jpg", MimeType = "image/jpeg" };
        await file.SaveFile(sourceStream, ct);
        await context.AddAsync(file, ct);
        await context.SaveChangesAsync(ct);

        Results<FileStreamHttpResult, NotFound, InternalServerError> result =
            await GetFileEndpoint.Handle(context, file.FileId, ct, width: 10, height: 10);

        FileStreamHttpResult fileResult = Assert.IsType<FileStreamHttpResult>(result.Result);
        Assert.Equal("image/jpeg", fileResult.ContentType);
        using MemoryStream buffer = new();
        await fileResult.FileStream.CopyToAsync(buffer, ct);
        buffer.Position = 0;
        ImageInfo info = await Image.IdentifyAsync(buffer, ct);
        Assert.Equal(10, info.Width);
        Assert.Equal(10, info.Height);

        Assert.True(System.IO.File.Exists(Path.Combine(_directory, "cache", "cover_10x10.jpg")));
    }

    [Fact]
    public async Task GetFile_IgnoresDimensionsForNonImageFiles()
    {
        await using MangaContext context = MangaContextFactory.Create();
        byte[] content = "file-bytes"u8.ToArray();
        DbFile file = new() { FileId = Guid.NewGuid(), Path = _directory, Name = "file.bin", MimeType = "application/octet-stream" };
        await file.SaveFile(new MemoryStream(content), ct);
        await context.AddAsync(file, ct);
        await context.SaveChangesAsync(ct);

        Results<FileStreamHttpResult, NotFound, InternalServerError> result =
            await GetFileEndpoint.Handle(context, file.FileId, ct, width: 10, height: 10);

        FileStreamHttpResult fileResult = Assert.IsType<FileStreamHttpResult>(result.Result);
        Assert.Equal("application/octet-stream", fileResult.ContentType);
        using MemoryStream buffer = new();
        await fileResult.FileStream.CopyToAsync(buffer, ct);
        Assert.Equal(content, buffer.ToArray());
    }

    [Fact]
    public async Task GetFile_Returns404ForUnknownId()
    {
        await using MangaContext context = MangaContextFactory.Create();

        Results<FileStreamHttpResult, NotFound, InternalServerError> result = await GetFileEndpoint.Handle(context, Guid.NewGuid(), ct);

        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public async Task GetFile_ReturnsInternalServerErrorWhenFileMissingOnDisk()
    {
        await using MangaContext context = MangaContextFactory.Create();
        DbFile file = new() { FileId = Guid.NewGuid(), Path = _directory, Name = "missing.bin", MimeType = "application/octet-stream" };
        await context.AddAsync(file, ct);
        await context.SaveChangesAsync(ct);

        Results<FileStreamHttpResult, NotFound, InternalServerError> result = await GetFileEndpoint.Handle(context, file.FileId, ct);

        Assert.IsType<InternalServerError>(result.Result);
    }
}
