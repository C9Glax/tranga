using System.ComponentModel.DataAnnotations;
using API.MangaConnectors;
using Microsoft.EntityFrameworkCore;

namespace API.Schema.MangaContext;

[PrimaryKey("Key")]
public class MangaConnectorId<T> : Identifiable where T : Identifiable
{
    public T Obj = null!;
    [StringLength(64)] public string ObjId { get; internal set; }

    [StringLength(32)] public string MangaConnectorName { get; private set; }

    [StringLength(256)] public string IdOnConnectorSite { get; init; }
    [Url] [StringLength(512)] public string? WebsiteUrl { get; internal init; }
    public bool UseForDownload { get; internal set; }

    public MangaConnectorId(T obj, string mangaConnectorName, string idOnConnectorSite, string? websiteUrl,
        bool useForDownload = false)
        : base(TokenGen.CreateToken(typeof(MangaConnectorId<T>), mangaConnectorName, idOnConnectorSite))
    {
        this.Obj = obj;
        this.ObjId = obj.Key;
        this.MangaConnectorName = mangaConnectorName;
        this.IdOnConnectorSite = idOnConnectorSite;
        this.WebsiteUrl = websiteUrl;
        this.UseForDownload = useForDownload;
    }

    public MangaConnectorId(T obj, MangaConnector mangaConnector, string idOnConnectorSite, string? websiteUrl, bool useForDownload = false)
        : this(obj, mangaConnector.Name, idOnConnectorSite, websiteUrl, useForDownload) { }

    /// <summary>
    /// EF CORE ONLY!!!
    /// </summary>
    public MangaConnectorId(string key, string objId, string mangaConnectorName, string idOnConnectorSite, bool useForDownload, string? websiteUrl)
        : base(key)
    {
        this.ObjId = objId;
        this.MangaConnectorName = mangaConnectorName;
        this.IdOnConnectorSite = idOnConnectorSite;
        this.WebsiteUrl = websiteUrl;
        this.UseForDownload = useForDownload;
    }

    public override string ToString() => $"{base.ToString()} {Obj}";
}