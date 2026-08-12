using Common.Helpers;

namespace Services.Manga.Database.Helpers;

/// <summary>
/// Helpers for reading and writing the on-disk contents referenced by a <see cref="DbFile"/>.
/// </summary>
public static class DbFileHelper
{
    /// <summary>
    /// Writes the given content to disk at the location described by <paramref name="file"/>, creating the
    /// containing directory if it does not already exist.
    /// </summary>
    /// <param name="file">The file entity describing where to save the content.</param>
    /// <param name="content">The content to write.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async Task SaveFile(this DbFile file, MemoryStream content, CancellationToken ct)
    {
        Directory.CreateDirectory(file.Path);
        await using FileStream fs = new(Path.Join(file.Path, file.Name), FileMode.Create, FileAccess.Write);
        content.Position = 0;
        await content.CopyToAsync(fs, ct);
        content.Position = 0;
        fs.Close();
    }

    /// <summary>
    /// Reads the on-disk contents referenced by <paramref name="file"/> into memory.
    /// </summary>
    /// <param name="file">The file entity describing what to load.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A stream positioned at the start of the file's contents.</returns>
    /// <exception cref="FileLoadException">File could not be loaded</exception>
    public static async Task<MemoryStream> LoadFile(this DbFile file, CancellationToken ct)
    {
        try
        {
            await using FileStream fs = new (Path.Join(file.Path, file.Name), FileMode.Open, FileAccess.Read);
            MemoryStream ms = new();
            await fs.CopyToAsync(ms, ct);
            ms.Position = 0;
            fs.Close();
            return ms;
        }
        catch
        {
            throw new FileLoadException();
        }
    }

    private const int MaxResizeDimension = 2048;

    /// <summary>
    /// Reads the on-disk contents referenced by <paramref name="file"/> into memory, resizing to
    /// <paramref name="width"/> x <paramref name="height"/> (cropping to fill, like CSS <c>object-fit: cover</c>)
    /// when both are given and <paramref name="file"/> is an image. Resized variants are cached on disk next to
    /// the original, so repeated requests for the same dimensions are served without re-encoding.
    /// </summary>
    /// <param name="file">The file entity describing what to load.</param>
    /// <param name="width">Target width in pixels, or null/non-positive to skip resizing.</param>
    /// <param name="height">Target height in pixels, or null/non-positive to skip resizing.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The (possibly resized) file contents, and the MIME type of the returned contents.</returns>
    /// <exception cref="FileLoadException">File could not be loaded</exception>
    public static async Task<(MemoryStream Stream, string MimeType)> LoadFile(this DbFile file, int? width, int? height, CancellationToken ct)
    {
        if (width is not > 0 || height is not > 0 || !file.MimeType.StartsWith("image/", StringComparison.Ordinal))
            return (await file.LoadFile(ct), file.MimeType);

        int targetWidth = Math.Min(width.Value, MaxResizeDimension);
        int targetHeight = Math.Min(height.Value, MaxResizeDimension);

        string cacheDirectory = Path.Join(file.Path, "cache");
        string cacheName = $"{Path.GetFileNameWithoutExtension(file.Name)}_{targetWidth}x{targetHeight}.jpg";
        string cachePath = Path.Join(cacheDirectory, cacheName);

        if (File.Exists(cachePath))
        {
            await using FileStream cachedFs = new(cachePath, FileMode.Open, FileAccess.Read);
            MemoryStream cached = new();
            await cachedFs.CopyToAsync(cached, ct);
            cached.Position = 0;
            return (cached, "image/jpeg");
        }

        MemoryStream original = await file.LoadFile(ct);
        TrangaImage resizable = new();
        await original.CopyToAsync(resizable, ct);
        MemoryStream resized = await resizable.AsJpeg(targetWidth, targetHeight, ct);

        Directory.CreateDirectory(cacheDirectory);
        resized.Position = 0;
        await using (FileStream cacheFs = new(cachePath, FileMode.Create, FileAccess.Write))
            await resized.CopyToAsync(cacheFs, ct);
        resized.Position = 0;

        return (resized, "image/jpeg");
    }
}
