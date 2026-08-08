using System.Net;
using System.Xml.Serialization;
using Common.Datatypes;
using Common.Helpers;
using Common.Tests.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;

namespace Common.Tests.Integration;

public class IntegrationTests : TrangaTest
{
    [Fact]
    public void LanguageWorksInSearchQueryViaImplicitConversion()
    {
        Language language = new("ja");

        SearchQuery query = new(Language: language);

        Assert.Equal("ja", query.Language);
    }

    // XmlSerializer refuses to process non-public types.
    public sealed record ConcreteComicInfo : ComicInfo;

    [Fact]
    public void ComicInfoRoundtripsAndConvertsToSearchQuery()
    {
        XmlSerializer serializer = new(typeof(ConcreteComicInfo), new XmlRootAttribute("ComicInfo"));
        ConcreteComicInfo original = new() { Title = "One Piece", Volume = 42 };

        using StringWriter writer = new();
        serializer.Serialize(writer, original);

        using StringReader reader = new(writer.ToString());
        ConcreteComicInfo? deserialized = (ConcreteComicInfo?)serializer.Deserialize(reader);

        Assert.NotNull(deserialized);
        Assert.Equal(original.Title, deserialized.Title);
        Assert.Equal(original.Volume, deserialized.Volume);

        SearchQuery query = deserialized.ToSearchQuery();
        Assert.Equal("One Piece", query.Title);
    }

    [Fact]
    public async Task RequestClientFetchesAndProcessesImages()
    {
        using Image<Rgba32> sourceImage = new(4, 4);
        using MemoryStream pngBytes = new();
        sourceImage.SaveAsPng(pngBytes);

        using MockHttpServer server = new(HttpStatusCode.OK, Convert.ToBase64String(pngBytes.ToArray()), "text/plain");
        RequestClient client = new();
        HttpRequestMessage request = new(HttpMethod.Get, server.BaseUrl);
        HttpResponseMessage response = await client.SendAsync(request, ct);
        string base64Body = await response.Content.ReadAsStringAsync(ct);

        TrangaImage image = new();
        await image.WriteAsync(Convert.FromBase64String(base64Body), ct);
        image.Position = 0;

        await image.Process(ct);

        IImageFormat format = await Image.DetectFormatAsync(image, ct);
        Assert.Equal("JPEG", format.Name);
    }

    [Fact]
    public void SettingsAndEnvVarsAreCoherent()
    {
        // Settings and EnvVars are independent static configuration surfaces read from
        // different environment variable names; mutating one must not affect the other.
        bool originalAllowNsfw = Common.Settings.Settings.AllowNSFW;
        try
        {
            Common.Settings.Settings.AllowNSFW = true;

            Assert.Equal("tranga", Common.Settings.EnvVars.DBName);
            Assert.True(Common.Settings.Settings.AllowNSFW);
        }
        finally
        {
            Common.Settings.Settings.AllowNSFW = originalAllowNsfw;
        }
    }

    [Theory]
    [InlineData("ongoing", false)]
    [InlineData("completed", true)]
    [InlineData("unknown", null)]
    public void ReleaseStatusParsingCanDriveFilteringLogic(string status, bool? expectedIsComplete)
    {
        ReleaseStatus? parsed = status.ParseStatus();

        bool? isComplete = parsed is null ? null : parsed == ReleaseStatus.Complete;

        Assert.Equal(expectedIsComplete, isComplete);
    }
}