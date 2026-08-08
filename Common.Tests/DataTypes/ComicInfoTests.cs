using System.Xml.Serialization;
using Common.Datatypes;

namespace Common.Tests.DataTypes;

public class ComicInfoTests
{
    // ComicInfo is abstract (generated from the ComicInfo.xsd schema); XmlSerializer cannot
    // (de)serialize an abstract root type directly, so tests exercise a concrete test subclass.
    // The subclass must be public: XmlSerializer refuses to process non-public types.
    public sealed record ConcreteComicInfo : ComicInfo;

    private static readonly XmlSerializer Serializer = new(typeof(ConcreteComicInfo), new XmlRootAttribute("ComicInfo"));

    private const string SampleXml = """
        <?xml version="1.0"?>
        <ComicInfo>
          <Title>My Title</Title>
          <Series>My Series</Series>
          <Number>3</Number>
          <Count>12</Count>
          <Volume>2</Volume>
          <Writer>Jane Doe</Writer>
          <Genre>Action</Genre>
          <PageCount>24</PageCount>
          <LanguageISO>en</LanguageISO>
          <BlackAndWhite>Yes</BlackAndWhite>
          <Manga>Yes</Manga>
          <AgeRating>Everyone 10+</AgeRating>
          <CommunityRating>4.5</CommunityRating>
          <Pages>
            <Page Image="0" ImageWidth="800" ImageHeight="1200" />
            <Page Image="1" DoublePage="true" />
          </Pages>
        </ComicInfo>
        """;

    [Fact]
    public void CanBeDeserializedFromXml()
    {
        using StringReader reader = new(SampleXml);
        ConcreteComicInfo? info = (ConcreteComicInfo?)Serializer.Deserialize(reader);

        Assert.NotNull(info);
        Assert.Equal("My Title", info.Title);
        Assert.Equal("My Series", info.Series);
        Assert.Equal("3", info.Number);
        Assert.Equal(12, info.Count);
        Assert.Equal(2, info.Volume);
        Assert.Equal("Jane Doe", info.Writer);
        Assert.Equal("Action", info.Genre);
        Assert.Equal(24, info.PageCount);
        Assert.Equal("en", info.LanguageIso);
        Assert.Equal(YesNo.Yes, info.BlackAndWhite);
        Assert.Equal(Manga.Yes, info.Manga);
        Assert.Equal(AgeRating.Everyone10, info.AgeRating);
        Assert.Equal(4.5m, info.CommunityRating);
        Assert.True(info.CommunityRatingSpecified);
    }

    [Fact]
    public void DeserializesPagesCollection()
    {
        using StringReader reader = new(SampleXml);
        ConcreteComicInfo? info = (ConcreteComicInfo?)Serializer.Deserialize(reader);

        Assert.NotNull(info);
        Assert.Equal(2, info.Pages.Count);
        Assert.Equal(0, info.Pages[0].Image);
        Assert.Equal(800, info.Pages[0].ImageWidth);
        Assert.Equal(1200, info.Pages[0].ImageHeight);
        Assert.False(info.Pages[0].DoublePage);
        Assert.Equal(1, info.Pages[1].Image);
        Assert.True(info.Pages[1].DoublePage);
    }

    [Fact]
    public void CanBeSerializedToXml()
    {
        ConcreteComicInfo info = new()
        {
            Title = "My Title",
            Series = "My Series",
            Volume = 2,
            Writer = "Jane Doe",
            PageCount = 24,
        };

        using StringWriter writer = new();
        Serializer.Serialize(writer, info);
        string xml = writer.ToString();

        Assert.Contains("<ComicInfo", xml);
        Assert.Contains("<Title>My Title</Title>", xml);
        Assert.Contains("<Series>My Series</Series>", xml);
        Assert.Contains("<Volume>2</Volume>", xml);
        Assert.Contains("<Writer>Jane Doe</Writer>", xml);
        Assert.Contains("<PageCount>24</PageCount>", xml);
    }

    [Fact]
    public void SerializedXmlIsWellFormedAndRoundtrips()
    {
        ConcreteComicInfo info = new() { Title = "My Title", Volume = 2 };
        info.Pages.Add(new ComicPageInfo { Image = 0, ImageWidth = 800, ImageHeight = 1200 });

        using StringWriter writer = new();
        Serializer.Serialize(writer, info);

        using StringReader reader = new(writer.ToString());
        ConcreteComicInfo? roundtripped = (ConcreteComicInfo?)Serializer.Deserialize(reader);

        Assert.NotNull(roundtripped);
        Assert.Equal(info.Title, roundtripped.Title);
        Assert.Equal(info.Volume, roundtripped.Volume);
        Assert.Single(roundtripped.Pages);
        Assert.Equal(800, roundtripped.Pages[0].ImageWidth);
    }

    [Fact]
    public void HasCorrectDefaultValues()
    {
        ConcreteComicInfo info = new();

        Assert.Equal("", info.Title);
        Assert.Equal("", info.Series);
        Assert.Equal("", info.Writer);
        Assert.Equal(-1, info.Count);
        Assert.Equal(-1, info.Volume);
        Assert.Equal(-1, info.AlternateCount);
        Assert.Equal(-1, info.Year);
        Assert.Equal(-1, info.Month);
        Assert.Equal(-1, info.Day);
        Assert.Equal(0, info.PageCount);
        Assert.Equal(YesNo.Unknown, info.BlackAndWhite);
        Assert.Equal(Manga.Unknown, info.Manga);
        Assert.Equal(AgeRating.Unknown, info.AgeRating);
        Assert.Equal(0m, info.CommunityRating);
        Assert.False(info.CommunityRatingSpecified);
        Assert.Empty(info.Pages);
        Assert.False(info.PagesSpecified);
    }
}