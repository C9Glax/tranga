namespace Common.Helpers;

public static class MangaCover
{
    public static async Task SaveCover(string filename, MemoryStream cover, CancellationToken ct)
    {
        Directory.CreateDirectory(Settings.Constants.CoverDirectory);
        await using FileStream fs = new(Path.Join(Settings.Constants.CoverDirectory, filename), FileMode.Create,
                   FileAccess.Write);
        cover.Position = 0;
        await cover.CopyToAsync(fs, ct);
    }

    public static async Task<MemoryStream?> LoadCover(string filename, CancellationToken ct)
    {
        try
        {
            await using FileStream fs = new (Path.Join(Settings.Constants.CoverDirectory, filename), FileMode.Open,
                FileAccess.Read);
            MemoryStream ms = new();
            await fs.CopyToAsync(ms, ct);
            ms.Position = 0;
            return ms;
        }
        catch (Exception e)
        {
            return null;
        }
    }
}