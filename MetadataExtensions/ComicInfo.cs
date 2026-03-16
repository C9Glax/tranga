namespace MetadataExtensions;

public abstract record ComicInfo : Common.Datatypes.ComicInfo
{
    public required Guid MetadataExtensionIdentifier { get; init; }
    public required MemoryStream Cover { get; init; }
}