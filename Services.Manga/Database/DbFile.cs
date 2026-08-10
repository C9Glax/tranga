using System.ComponentModel.DataAnnotations.Schema;

namespace Services.Manga.Database;

/// <summary>
/// Database entity referencing a file stored on disk (e.g. a chapter archive or cover image).
/// </summary>
public sealed record DbFile
{
    /// <summary>The unique identifier of the file.</summary>
    public Guid FileId { get; init; }

    /// <summary>The directory path the file is stored in, relative to or as configured by the persistent storage volume.</summary>
    public required string Path { get; init; }

    /// <summary>The file name, including extension.</summary>
    public required string Name { get; init; }

    /// <summary>The MIME type of the file's contents.</summary>
    public required string MimeType { get; init; }

    /// <summary>The full path to the file, combining <see cref="Path"/> and <see cref="Name"/>. Not mapped to the database.</summary>
    [NotMapped] public string FullPath => System.IO.Path.Join(Path, Name);
}
