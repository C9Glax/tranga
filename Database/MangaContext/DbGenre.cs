using System.ComponentModel.DataAnnotations;

namespace Database.MangaContext;

public sealed record DbGenre
{
    [StringLength(128)]
    public required string Genre { get; init; }

    #region Navigations

    public ICollection<DbMetadata>? MetadataEntries { get; init; }

    #endregion
}