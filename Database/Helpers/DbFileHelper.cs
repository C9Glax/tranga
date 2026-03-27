using Database.MangaContext;

namespace Database.Helpers;

public static class DbFileHelper
{
    public static async Task SaveFile(this DbFile file, MemoryStream content, CancellationToken ct)
    {
        Directory.CreateDirectory(file.Path);
        await using FileStream fs = new(Path.Join(file.Path, file.Name), FileMode.Create, FileAccess.Write);
        content.Position = 0;
        await content.CopyToAsync(fs, ct);
        content.Position = 0;
        fs.Close();
    }

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