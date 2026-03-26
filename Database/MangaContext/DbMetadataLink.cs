using Common.Datatypes;

namespace Database.MangaContext;

public sealed class DbMetadataLink
{
    public Guid Id { get; init; }
    
    public required Guid MangaId { get; init; }
    
    public DbManga? Manga { get; init; }
    
    public required Guid MetadataExtensionId { get; init; }
    
    public required string Identifier { get; init; }
    
    public Guid? CoverId { get; init; }
    
    public DbFile? Cover { get; set; }
    
    public Status Status { get; init; }
    
    public Rating? Rating { get; init; }
    
    public Demographic? Demographic { get; init; }
    
    public string? Url { get; init; }
    
    public string? Summary { get; init; }

    public int Year { get; init; }

    public int Month { get; init; }

    public int Day { get; init; }
    
    public string? Language { get; init; }
    
    public ICollection<DbPerson>? Authors { get; init; } 
    
    public ICollection<DbPerson>? Artists { get; init; } 
    
    public ICollection<DbGenre>? Genres { get; init; } 
}

public enum Status : byte
{
    Releasing = 0,
    Finished = 1,
    Hiatus = 2,
    Cancelled = 3,
    Pending = 4
}

// ReSharper disable twice InconsistentNaming
public enum Rating : byte
{
    SFW = 0,
    NSFW = 1
}

public enum Demographic : byte
{
    Josei = 0,
    Lolicon = 1,
    Seinen = 2,
    Shotacon = 3,
    Shoujo = 4,
    ShoujoAi = 5,
    Shounen = 6,
    ShounenAi = 7,
    Yaoi = 8,
    Yuri = 9
}