namespace Services.Manga.Entities.MetadataExtensions;

public interface IMetadataExtension
{
    public Guid MetadataExtensionId { get; }
    public string Name { get; }
    public string IconUrl { get; }
}


public sealed record MangaDex : IMetadataExtension
{
    public Guid MetadataExtensionId => Guid.Parse("019ce521-deaf-7739-9e14-eb6f4afc86e2");
    public string Name => "MangaDex";
    public string IconUrl => "https://mangadex.org/img/brand/mangadex-logo.svg";
};

public sealed record MangaUpdates : IMetadataExtension
{
    public Guid MetadataExtensionId => Guid.Parse("019cf2cb-3aac-7c9c-9580-7091471b6788");
    public string Name => "MangaUpdates";
    public string IconUrl => "https://www.mangaupdates.com/images/manga-updates.svg";
}

public sealed record MangaPlus : IMetadataExtension
{
    public Guid MetadataExtensionId => Guid.Parse("0bc30bc2-dbcf-47ce-a890-c8428a7e031b");
    public string Name => "MangaPlus";
    public string IconUrl => "https://mangaplus.shueisha.co.jp/apple-touch-icon.png";
}