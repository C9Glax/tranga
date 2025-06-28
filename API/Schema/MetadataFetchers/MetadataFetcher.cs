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
    
    public abstract MetadataEntry? FindLinkedMetadataEntry(Manga manga);

    public bool TryGetMetadataEntry(Manga manga, [NotNullWhen(true)] out MetadataEntry? metadataEntry)
    {
        metadataEntry = FindLinkedMetadataEntry(manga);
        return metadataEntry != null;
    }

    /// <summary>
    /// Updates the Manga linked in the MetadataEntry
    /// </summary>
    public abstract void UpdateMetadata(MetadataEntry metadataEntry, PgsqlContext dbContext);
}