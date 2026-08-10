namespace Services.Manga.Entities.DownloadExtensions;

/// <summary>
/// Identifies a download source extension (e.g. MangaDex, WeebCentral) that manga/chapters can be attributed to.
/// </summary>
public interface IDownloadExtension
{
    /// <summary>The stable identifier of the download extension, matching <c>IExtension.Identifier</c> in the Extensions project.</summary>
    public Guid DownloadExtensionsId { get; }
    /// <summary>The human-readable name of the download extension.</summary>
    public string Name { get; }
    /// <summary>URL to the download extension's icon/logo.</summary>
    public string IconUrl { get; }
}

/// <summary>The MangaDex download extension.</summary>
public sealed record MangaDex : IDownloadExtension
{
    /// <inheritdoc />
    public Guid DownloadExtensionsId => Guid.Parse("019ce521-deaf-7739-9e14-eb6f4afc86e2");
    /// <inheritdoc />
    public string Name => "MangaDex";
    /// <inheritdoc />
    public string IconUrl => "https://mangadex.org/img/brand/mangadex-logo.svg";
};

/// <summary>The WeebCentral download extension.</summary>
public sealed record WeebCentral : IDownloadExtension
{
    /// <inheritdoc />
    public Guid DownloadExtensionsId => Guid.Parse("0199a6b1-1c6f-7d2a-9a3e-3a9e6c5b1f10");
    /// <inheritdoc />
    public string Name => "WeebCentral";
    /// <inheritdoc />
    public string IconUrl => "https://weebcentral.com/static/images/apple-touch-icon.png";
};

/// <summary>The AsuraScans download extension.</summary>
public sealed record AsuraScans : IDownloadExtension
{
    /// <inheritdoc />
    public Guid DownloadExtensionsId => Guid.Parse("0199a6e4-2b7a-7f1e-9c4a-5e2d8b6c1a30");
    /// <inheritdoc />
    public string Name => "AsuraScans";
    /// <inheritdoc />
    public string IconUrl => "https://asurascans.com/images/logo.webp";
};

/// <summary>The MangaPlus download extension.</summary>
public sealed record MangaPlus : IDownloadExtension
{
    /// <inheritdoc />
    public Guid DownloadExtensionsId => Guid.Parse("0bc30bc2-dbcf-47ce-a890-c8428a7e031b");
    /// <inheritdoc />
    public string Name => "MangaPlus";
    /// <inheritdoc />
    public string IconUrl => "https://mangaplus.shueisha.co.jp/apple-touch-icon.png";
};