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