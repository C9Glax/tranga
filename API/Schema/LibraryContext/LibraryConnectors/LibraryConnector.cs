using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using log4net;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace API.Schema.LibraryContext.LibraryConnectors;

[PrimaryKey("LibraryConnectorId")]
public abstract class LibraryConnector(string libraryConnectorId, LibraryType libraryType, string baseUrl, string auth)
{
    [StringLength(64)]
    [Required]
    public string LibraryConnectorId { get; } = libraryConnectorId;

    [Required]
    public LibraryType LibraryType { get; init; } = libraryType;
    [StringLength(256)]
    [Required]
    [Url]
    public string BaseUrl { get; init; } = baseUrl;
    [StringLength(256)]
    [Required]
    public string Auth { get; init; } = auth;
    
    [JsonIgnore]
    [NotMapped]
    protected ILog Log { get; init; } = LogManager.GetLogger($"{libraryType.ToString()} {baseUrl}");
    
    protected abstract void UpdateLibraryInternal();
    internal abstract bool Test();
}