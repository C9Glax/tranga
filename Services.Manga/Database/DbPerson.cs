using System.ComponentModel.DataAnnotations;

namespace Services.Manga.Database;

public sealed record DbPerson
{
    [StringLength(128)]
    public required string Name { get; init; }
}