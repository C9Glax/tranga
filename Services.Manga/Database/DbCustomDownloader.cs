using System.ComponentModel.DataAnnotations;

namespace Services.Manga.Database;

/// <summary>
/// Database entity for a user-defined, selector based download source (see
/// <c>Extensions.Custom.SelectorDownloaderDefinition</c>). Stored in the manga database so that both the manga and
/// the tasks service can load it.
/// </summary>
public sealed record DbCustomDownloader
{
    /// <summary>The id of the source, which is also the identifier of the download extension it provides.</summary>
    public required Guid CustomDownloaderId { get; init; }

    /// <summary>Display name of the source (duplicated from the definition for listing).</summary>
    [StringLength(256)]
    public required string Name { get; set; }

    /// <summary>The source definition as JSON (<c>SelectorDownloaderJson</c>).</summary>
    public required string DefinitionJson { get; set; }

    /// <summary>When the source was created.</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>When the source was last changed.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
