using SixLabors.ImageSharp;

namespace Common.Helpers;

public class TrangaImage : MemoryStream;

public static class ImageHelper
{
    /// <summary>
    /// Processes Images in-place
    /// </summary>
    /// <param name="image"></param>
    /// <param name="ct"></param>
    public static async Task Process(this TrangaImage image, CancellationToken ct)
    {
        await image.ToJpeg(ct);
    }
    
    /// <exception cref="ArgumentNullException">The stream is null</exception>
    /// <exception cref="NotSupportedException">The stream is not readable or the image format is not supported.</exception>
    /// <exception cref="InvalidImageContentException">The encoded image contains invalid content.</exception>
    /// <exception cref="UnknownImageFormatException">The encoded image format is unknown.</exception>
    public static async Task ToJpeg(this TrangaImage memoryStream, CancellationToken ct)
    {
        memoryStream.Position = 0;
        using Image image = await Image.LoadAsync(memoryStream, ct);
        memoryStream.Position = 0;
        await image.SaveAsJpegAsync(memoryStream, ct);
        memoryStream.Position = 0;
    }

    /// <exception cref="ArgumentNullException">The stream is null</exception>
    /// <exception cref="NotSupportedException">The stream is not readable or the image format is not supported.</exception>
    /// <exception cref="InvalidImageContentException">The encoded image contains invalid content.</exception>
    /// <exception cref="UnknownImageFormatException">The encoded image format is unknown.</exception>
    public static async Task<MemoryStream> AsJpeg(this TrangaImage memoryStream, CancellationToken ct)
    {
        memoryStream.Position = 0;
        using Image image = await Image.LoadAsync(memoryStream, ct);
        memoryStream.Position = 0;
        MemoryStream ret = new();
        await image.SaveAsJpegAsync(memoryStream, ct);
        ret.Position = 0;
        return ret;
    }
}