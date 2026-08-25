namespace Common.Datatypes;

/// <summary>
/// Concrete <see cref="ComicInfo"/> subclass. The generated base type is abstract, but
/// <see cref="System.Xml.Serialization.XmlSerializer"/> requires a concrete public root type to (de)serialize.
/// </summary>
public sealed record ConcreteComicInfo : ComicInfo;
