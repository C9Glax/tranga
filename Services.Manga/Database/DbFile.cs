using System.ComponentModel.DataAnnotations.Schema;

namespace Services.Manga.Database;

public sealed record DbFile
{
    public Guid FileId { get; init; }

    public required string Path { get; init; }

    public required string Name { get; init; }

    public required string MimeType { get; init; }

    [NotMapped] public string FullPath => System.IO.Path.Join(Path, Name);
}