using Common.Datatypes;

namespace MetadataExtensions;

public abstract record ComicInfo : Common.Datatypes.ComicInfo
{
    public required MemoryStream Cover { get; init; }
}