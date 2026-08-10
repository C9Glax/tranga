using System.ComponentModel.DataAnnotations;

namespace Services.Manga.Database;

/// <summary>
/// Database entity representing a genre tag (e.g. "Action", "Romance") that metadata entries can be associated with.
/// </summary>
public sealed record DbGenre
{
    /// <summary>The genre name, used as the primary key.</summary>
    [StringLength(128)]
    public required string Genre { get; init; }

    #region Navigations

    /// <summary>The metadata entries tagged with this genre.</summary>
    public ICollection<DbMetadata>? MetadataEntries { get; init; }

    #endregion
}
