using System.ComponentModel.DataAnnotations;
using API.Schema.MangaConnectors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;

namespace API.Schema;

[PrimaryKey("MangaId", "MangaConnectorName")]
public class MangaConnectorMangaEntry
{
    [StringLength(64)] [Required] public string MangaId { get; private set; } = null!;
    [JsonIgnore] private Manga? _manga = null!;

    [JsonIgnore]
    public Manga Manga
    {
        get => _lazyLoader.Load(this, ref _manga) ?? throw new InvalidOperationException();
        init => _manga = value;
    }

    [StringLength(32)] [Required] public string MangaConnectorName { get; private set; } = null!;
    [JsonIgnore] private MangaConnector? _mangaConnector = null!;
    [JsonIgnore]
    public MangaConnector MangaConnector
    {
        get => _lazyLoader.Load(this, ref _mangaConnector) ?? throw new InvalidOperationException();
        init
        {
            MangaConnectorName = value.Name;
            _mangaConnector = value;
        }
    }

    [StringLength(256)] [Required] public string IdOnConnectorSite { get; init; }
    [Url] [StringLength(512)] [Required] public string WebsiteUrl { get; internal init; }
    
    private readonly ILazyLoader _lazyLoader = null!;

    public MangaConnectorMangaEntry(Manga manga, MangaConnector mangaConnector, string idOnConnectorSite, string websiteUrl)
    {
        this.Manga = manga;
        this.MangaConnector = mangaConnector;
        this.IdOnConnectorSite = idOnConnectorSite;
        this.WebsiteUrl = websiteUrl;
    }

    /// <summary>
    /// EF CORE ONLY!!!
    /// </summary>
    public MangaConnectorMangaEntry(ILazyLoader lazyLoader, string mangaId, string mangaConnectorName, string idOnConnectorSite, string websiteUrl)
    {
        this._lazyLoader = lazyLoader;
        this.MangaId = mangaId;
        this.MangaConnectorName = mangaConnectorName;
        this.IdOnConnectorSite = idOnConnectorSite;
        this.WebsiteUrl = websiteUrl;
    }
}