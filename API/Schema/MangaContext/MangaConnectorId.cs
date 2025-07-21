using System.ComponentModel.DataAnnotations;
using API.MangaConnectors;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace API.Schema.MangaContext;

[PrimaryKey("Key")]
public class MangaConnectorId<T> : Identifiable where T : Identifiable
{
    [StringLength(64)] [Required] public string ObjId { get; private set; } = null!;
    [JsonIgnore] public T Obj = null!;

    [StringLength(32)] [Required] public string MangaConnectorName { get; private set; } = null!;

    [StringLength(256)] [Required] public string IdOnConnectorSite { get; init; }
    [Url] [StringLength(512)] public string? WebsiteUrl { get; internal init; }
    public bool UseForDownload { get; internal set; }

    public MangaConnectorId(T obj, MangaConnector mangaConnector, string idOnConnectorSite, string? websiteUrl, bool useForDownload = false)
        : base(TokenGen.CreateToken(typeof(MangaConnectorId<T>), mangaConnector.Name, idOnConnectorSite))
    {
        this.Obj = obj;
        this.MangaConnectorName = mangaConnector.Name;
        this.IdOnConnectorSite = idOnConnectorSite;
        this.WebsiteUrl = websiteUrl;
        this.UseForDownload = useForDownload;
    }

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