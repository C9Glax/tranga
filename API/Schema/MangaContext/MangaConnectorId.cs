using System.ComponentModel.DataAnnotations;
using API.Schema.MangaContext.MangaConnectors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;

namespace API.Schema.MangaContext;

[PrimaryKey("Key")]
public class MangaConnectorId<T> : Identifiable where T : Identifiable
{
    [StringLength(64)] [Required] public string ObjId { get; private set; } = null!;
    [JsonIgnore] private T? _obj;

    [JsonIgnore]
    public T Obj
    {
        get => _lazyLoader.Load(this, ref _obj) ?? throw new InvalidOperationException();
        internal set
        {
            ObjId = value.Key;
            _obj = value;
        }
    }

    [StringLength(32)] [Required] public string MangaConnectorName { get; private set; } = null!;
    [JsonIgnore] private MangaConnector? _mangaConnector;
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
    [Url] [StringLength(512)] public string? WebsiteUrl { get; internal init; }
    public bool UseForDownload { get; internal set; }
    
    private readonly ILazyLoader _lazyLoader = null!;

    public MangaConnectorId(T obj, MangaConnector mangaConnector, string idOnConnectorSite, string? websiteUrl, bool useForDownload = false)
        : base(TokenGen.CreateToken(typeof(MangaConnectorId<T>), mangaConnector.Name, idOnConnectorSite))
    {
        this.Obj = obj;
        this.MangaConnector = mangaConnector;
        this.IdOnConnectorSite = idOnConnectorSite;
        this.WebsiteUrl = websiteUrl;
        this.UseForDownload = useForDownload;
    }

    /// <summary>
    /// EF CORE ONLY!!!
    /// </summary>
    public MangaConnectorId(ILazyLoader lazyLoader, string key, string objId, string mangaConnectorName, string idOnConnectorSite, bool useForDownload, string? websiteUrl)
        : base(key)
    {
        this._lazyLoader = lazyLoader;
        this.ObjId = objId;
        this.MangaConnectorName = mangaConnectorName;
        this.IdOnConnectorSite = idOnConnectorSite;
        this.WebsiteUrl = websiteUrl;
        this.UseForDownload = useForDownload;
    }

    public override string ToString() => $"{base.ToString()} {_obj}";
}