namespace Database.MangaContext;

public sealed record DbManga
{
    public Guid Id { get; init; }
    
    public required string Series { get; init; }

    public required bool Monitor { get; set; }
    
    public ICollection<DbDownloadLink>? DownloadLinks { get; init; }
    
    public ICollection<DbMetadataLink>? MetadataLinks { get; init; }
}