using Common.Data;
using Data;

namespace MetadataExtensions;

public interface IMetadataExtension
{
     public Task<List<ComicInfo>?> Search(SearchQuery searchQuery);
}