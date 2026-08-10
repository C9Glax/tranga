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
}
