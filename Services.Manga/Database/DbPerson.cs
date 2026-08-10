using System.ComponentModel.DataAnnotations;

namespace Services.Manga.Database;

/// <summary>
/// Database entity representing a person (author or artist) that metadata entries can be credited to.
/// </summary>
public sealed record DbPerson
{
    /// <summary>The person's name, used as the primary key.</summary>
    [StringLength(128)]
    public required string Name { get; init; }
}
