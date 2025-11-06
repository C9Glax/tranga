using log4net;
using Microsoft.EntityFrameworkCore;

namespace API.Schema.MangaContext.MetadataFetchers;

[PrimaryKey("Name")]
public abstract class MetadataFetcher
{
    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public string Name { get; init; }

    protected ILog Log;

    protected MetadataFetcher()
    {
        this.Name = this.GetType().Name;
        this.Log = LogManager.GetLogger(Name);
    }
    
    /// <summary>
    /// EFCORE ONLY!!!
    /// </summary>
    internal MetadataFetcher(string name)
    {
        this.Name = name;
        this.Log = LogManager.GetLogger(Name);
    }

    internal MetadataEntry CreateMetadataEntry(Manga manga, string identifier) =>
        new (this, manga, identifier);
    
    public abstract MetadataSearchResult[] SearchMetadataEntry(Manga manga);
    
    public abstract MetadataSearchResult[] SearchMetadataEntry(string searchTerm);

    /// <summary>
    /// Updates the Manga linked in the MetadataEntry
    /// </summary>
    public abstract Task UpdateMetadata(MetadataEntry metadataEntry, MangaContext dbContext, CancellationToken token);
}