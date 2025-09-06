using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using log4net;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace API.Schema.LibraryContext.LibraryConnectors;

[PrimaryKey("Key")]
public abstract class LibraryConnector : Identifiable
{
    public LibraryType LibraryType { get; init; }
    public string BaseUrl { get; init; }
    public string Auth { get; init; }
    protected ILog Log { get; init; }

    protected LibraryConnector(LibraryType libraryType, string baseUrl, string auth)
        : base()
    {
        this.LibraryType = libraryType;
        this.BaseUrl = baseUrl;
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

    public override string ToString() => $"{base.ToString()} {this.LibraryType} {this.BaseUrl}";
    
    protected abstract void UpdateLibraryInternal();
    internal abstract bool Test();
}

public enum LibraryType : byte
{
    Komga = 0,
    Kavita = 1
}