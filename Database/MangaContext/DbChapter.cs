namespace Database.MangaContext;

public sealed record DbChapter
{
    public Guid Id { get; init; }
    
    public required Guid DownloadLinkId { get; init; }
    
    public DbDownloadLink? DownloadLink { get; init; }
    
    public required Guid DownloadExtensionId { get; init; }
    
    public required string Identifier { get; init; }
    
    public string? Volume { get; init; }
    
    public required string Chapter { get; init; }
    
    public string? Url { get; init; }
    
    public required bool Download { get; set; }
    
    public Guid? FileId { get; set; }
    
    public DbFile? File { get; set; }
}