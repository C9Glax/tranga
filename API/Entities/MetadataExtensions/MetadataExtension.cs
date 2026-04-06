namespace API.Entities.MetadataExtensions;

public interface IMetadataExtension
{
    public Guid MetadataExtensionId { get; }
    public string Name { get; }
    public string IconUrl { get; }
}


public sealed record MangaDex : IMetadataExtension
{
    public Guid MetadataExtensionId => Guid.Parse("019d6340-9787-79fc-82cb-4dae3383e8af");
    public string Name => "MangaDex";
    public string IconUrl => "https://mangadex.org/img/brand/mangadex-logo.svg";
};

public sealed record MangaUpdates : IMetadataExtension
{
    public Guid MetadataExtensionId => Guid.Parse("019cf2cb-3aac-7c9c-9580-7091471b6788");
    public string Name => "MangaUpdates";
    public string IconUrl => "https://www.mangaupdates.com/images/manga-updates.svg";
}