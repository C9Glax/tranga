using Common.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;

namespace Common.Tests.Helpers;

public sealed class ImageHelperTests : TrangaTest
{
    private static TrangaImage CreatePng()
    {
        using Image<Rgba32> image = new(4, 4);
        TrangaImage stream = new();
        image.SaveAsPng(stream);
        stream.Position = 0;
        return stream;
    }

    [Fact]
    public async Task ProcessConvertsImageToJpegInPlace()
    {
        TrangaImage image = CreatePng();

        await image.Process(ct);

        Assert.Equal(0, image.Position);
        IImageFormat format = await Image.DetectFormatAsync(image, ct);
        Assert.Equal("JPEG", format.Name);
    }

    [Fact]
    public async Task ToJpegConvertsSuccessfullyAndResetsPosition()
    {
        TrangaImage image = CreatePng();

        await image.ToJpeg(ct);

        Assert.Equal(0, image.Position);
        IImageFormat format = await Image.DetectFormatAsync(image, ct);
        Assert.Equal("JPEG", format.Name);
    }

    [Fact]
    public async Task AsJpegReturnsNewStreamWithoutModifyingOriginal()
    {
        TrangaImage image = CreatePng();
        byte[] originalBytes = image.ToArray();

        MemoryStream result = await image.AsJpeg(ct);

        Assert.NotSame(image, result);
        Assert.Equal(0, result.Position);
        Assert.NotEmpty(result.ToArray());

        IImageFormat format = await Image.DetectFormatAsync(result, ct);
        Assert.Equal("JPEG", format.Name);

        Assert.Equal(originalBytes, image.ToArray());
    }

    [Fact]
    public async Task AsJpegWithDimensionsResizesToExactTargetSize()
    {
        using Image<Rgba32> image = new(40, 20);
        TrangaImage stream = new();
        image.SaveAsPng(stream);
        stream.Position = 0;

        MemoryStream result = await stream.AsJpeg(10, 10, ct);

        IImageFormat format = await Image.DetectFormatAsync(result, ct);
        Assert.Equal("JPEG", format.Name);

        result.Position = 0;
        ImageInfo info = await Image.IdentifyAsync(result, ct);
        Assert.Equal(10, info.Width);
        Assert.Equal(10, info.Height);
    }

    [Theory]
    [InlineData("png")]
    [InlineData("bmp")]
    [InlineData("gif")]
    [InlineData("webp")]
    public async Task HandlesMultipleImageFormats(string sourceFormat)
    {
        using Image<Rgba32> image = new(4, 4);
        TrangaImage stream = new();
        switch (sourceFormat)
        {
            case "png": image.SaveAsPng(stream); break;
            case "bmp": image.SaveAsBmp(stream); break;
            case "gif": image.SaveAsGif(stream); break;
            case "webp": image.SaveAsWebp(stream); break;
        }
        stream.Position = 0;

        await stream.ToJpeg(ct);

        IImageFormat format = await Image.DetectFormatAsync(stream, ct);
        Assert.Equal("JPEG", format.Name);
    }

    [Fact]
    public async Task ThrowsOnUnsupportedOrInvalidImageContent()
    {
        TrangaImage stream = new();
        stream.Write([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);

        await Assert.ThrowsAsync<UnknownImageFormatException>(() => stream.ToJpeg(ct));
    }
}