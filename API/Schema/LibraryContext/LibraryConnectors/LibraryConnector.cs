using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using log4net;
using Microsoft.EntityFrameworkCore;

namespace API.Schema.LibraryContext.LibraryConnectors;

[PrimaryKey("Key")]
public abstract class LibraryConnector : Identifiable
{
    public LibraryType LibraryType { get; init; }
    [StringLength(256)] [Url] public string BaseUrl { get; init; }
    [StringLength(256)] public string Auth { get; init; }
    [NotMapped] protected ILog Log { get; init; }

    protected LibraryConnector(LibraryType libraryType, string baseUrl, string auth)
        : base()
    {
        this.LibraryType = libraryType;
        this.BaseUrl = baseUrl.TrimEnd('/', ' ');
        this.Auth = auth;
        this.Log = LogManager.GetLogger(GetType());
    }

    /// <summary>
    /// EF CORE ONLY!!!!
    /// </summary>
    internal LibraryConnector(string key, LibraryType libraryType, string baseUrl, string auth)
        : base(key)
    {
        this.LibraryType = libraryType;
        this.BaseUrl = baseUrl;
        this.Auth = auth;
        this.Log = LogManager.GetLogger(GetType());
    }

    protected Uri BuildUri(string relativePath) => Utils.BuildUri(BaseUrl, relativePath);

    public override string ToString() => $"{base.ToString()} {this.LibraryType} {this.BaseUrl}";
    
    public abstract Task UpdateLibrary(CancellationToken ct);
    internal abstract Task<bool> Test(CancellationToken ct);
}