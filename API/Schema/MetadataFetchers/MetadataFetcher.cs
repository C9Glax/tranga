using System.Diagnostics.CodeAnalysis;
using API.Schema.Contexts;
using Microsoft.EntityFrameworkCore;

namespace API.Schema.MetadataFetchers;

[PrimaryKey("MetadataFetcherName")]
public abstract class MetadataFetcher
{
    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public string MetadataFetcherName { get; init; }

    protected MetadataFetcher()
    {
        this.MetadataFetcherName = this.GetType().Name;
    }
    
    /// <summary>
    /// EFCORE ONLY!!!
    /// </summary>
    internal MetadataFetcher(string metadataFetcherName)
    {
        this.MetadataFetcherName = metadataFetcherName;
    }

    internal MetadataEntry CreateMetadataEntry(Manga manga, string identifier) =>
        new (this, manga, identifier);
    
    public abstract MetadataSearchResult[] SearchMetadataEntry(Manga manga);
    
    public abstract MetadataSearchResult[] SearchMetadataEntry(string searchTerm);

    /// <summary>
    /// Updates the Manga linked in the MetadataEntry
    /// </summary>
    public abstract void UpdateMetadata(MetadataEntry metadataEntry, PgsqlContext dbContext);
}