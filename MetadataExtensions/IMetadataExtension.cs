namespace MetadataExtensions;

public interface IMetadataExtension
{
     /// <summary>
     /// The unique Extension Identifier
     /// </summary>
     public Guid Identifier { get; init; }
     
     /// <summary>
     /// The Url used by the <see cref="IMetadataExtension"/>
     /// </summary>
     public string BaseUrl { get; init; }
     
     /// <summary>
     /// The Name of the <see cref="IMetadataExtension"/>
     /// </summary>
     public string Name { get; init; }

     /// <summary>
     /// Searches the Metadata-Provider using the <paramref name="searchQuery"/>
     /// </summary>
     /// <param name="searchQuery">Query to use for searching Manga</param>
     /// <param name="ct">The Cancellation Token for the Task</param>
     /// <returns>A Task representing the long running operation.</returns>
     public Task<List<ComicInfo>?> Search(Common.Datatypes.SearchQuery searchQuery, CancellationToken ct);
}