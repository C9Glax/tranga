namespace Services.Manga.Entities.DownloadExtensions;

public interface IDownloadExtension
{
    public Guid DownloadExtensionsId { get; } 
    public string Name { get; }
    public string IconUrl { get; } 
}

public sealed record MangaDex : IDownloadExtension
{
    public Guid DownloadExtensionsId => Guid.Parse("019ce521-deaf-7739-9e14-eb6f4afc86e2");
    public string Name => "MangaDex";
    public string IconUrl => "https://mangadex.org/img/brand/mangadex-logo.svg";
};

public sealed record WeebCentral : IDownloadExtension
{
    public Guid DownloadExtensionsId => Guid.Parse("0199a6b1-1c6f-7d2a-9a3e-3a9e6c5b1f10");
    public string Name => "WeebCentral";
    public string IconUrl => "https://weebcentral.com/static/images/apple-touch-icon.png";
};

public sealed record AsuraScans : IDownloadExtension
{
    public Guid DownloadExtensionsId => Guid.Parse("0199a6e4-2b7a-7f1e-9c4a-5e2d8b6c1a30");
    public string Name => "AsuraScans";
    public string IconUrl => "https://asurascans.com/images/logo.webp";
};