namespace Services.Manga.Entities.MetadataExtensions;

/// <summary>
/// Identifies a metadata source extension (e.g. MangaDex, MangaUpdates) that manga metadata entries can be attributed to.
/// </summary>
public interface IMetadataExtension
{
    /// <summary>The stable identifier of the metadata extension, matching <c>IExtension.Identifier</c> in the Extensions project.</summary>
    public Guid MetadataExtensionId { get; }
    /// <summary>The human-readable name of the metadata extension.</summary>
    public string Name { get; }
    /// <summary>URL to the metadata extension's icon/logo.</summary>
    public string IconUrl { get; }
}


/// <summary>The MangaDex metadata extension.</summary>
public sealed record MangaDex : IMetadataExtension
{
    /// <inheritdoc />
    public Guid MetadataExtensionId => Guid.Parse("019ce521-deaf-7739-9e14-eb6f4afc86e2");
    /// <inheritdoc />
    public string Name => "MangaDex";
    /// <inheritdoc />
    public string IconUrl => "https://mangadex.org/img/brand/mangadex-logo.svg";
};

/// <summary>The MangaUpdates metadata extension.</summary>
public sealed record MangaUpdates : IMetadataExtension
{
    /// <inheritdoc />
    public Guid MetadataExtensionId => Guid.Parse("019cf2cb-3aac-7c9c-9580-7091471b6788");
    /// <inheritdoc />
    public string Name => "MangaUpdates";
    /// <inheritdoc />
    public string IconUrl => "https://www.mangaupdates.com/images/manga-updates.svg";
}

/// <summary>The MangaPlus metadata extension.</summary>
public sealed record MangaPlus : IMetadataExtension
{
    /// <inheritdoc />
    public Guid MetadataExtensionId => Guid.Parse("0bc30bc2-dbcf-47ce-a890-c8428a7e031b");
    /// <inheritdoc />
    public string Name => "MangaPlus";
    /// <inheritdoc />
    public string IconUrl => "https://mangaplus.shueisha.co.jp/apple-touch-icon.png";
}