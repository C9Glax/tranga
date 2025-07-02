using Microsoft.EntityFrameworkCore;

namespace API.Schema.MangaContext.MetadataFetchers;

[PrimaryKey("Name")]
public abstract class MetadataFetcher
{
    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public string Name { get; init; }

    protected MetadataFetcher()
    {
        this.Name = this.GetType().Name;
    }
    
    /// <summary>
    /// EFCORE ONLY!!!
    /// </summary>
    internal MetadataFetcher(string name)
    {
        this.Name = name;
    }

    internal MetadataEntry CreateMetadataEntry(Manga manga, string identifier) =>
        new (this, manga, identifier);
    
    public abstract MetadataSearchResult[] SearchMetadataEntry(Manga manga);
    
    public abstract MetadataSearchResult[] SearchMetadataEntry(string searchTerm);

    /// <summary>
    /// Updates the Manga linked in the MetadataEntry
    /// </summary>
    public abstract void UpdateMetadata(MetadataEntry metadataEntry, MangaContext dbContext);
}