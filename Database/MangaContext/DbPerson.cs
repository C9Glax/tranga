using System.ComponentModel.DataAnnotations;

namespace Database.MangaContext;

public sealed record DbPerson
{
    [StringLength(128)]
    public required string Name { get; init; }
}