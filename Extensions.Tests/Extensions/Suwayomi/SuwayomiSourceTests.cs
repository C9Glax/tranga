using Extensions.Extensions.Suwayomi;

namespace Extensions.Tests.Extensions.Suwayomi;

/// <summary>
/// Offline tests for the pieces of <see cref="SuwayomiSource"/> that must hold without a sidecar: identifier
/// derivation (which is persisted against download links) and url parsing.
/// Live behaviour is covered by <see cref="SuwayomiSidecarTests"/>, which skips when the sidecar is switched off.
/// </summary>
public sealed class SuwayomiSourceTests : Common.Tests.TrangaTest
{
    private const string BatoSourceId = "4796850560";
    private const string ComickSourceId = "1234567890123";

    private static SuwayomiSource CreateSource(string sourceId = BatoSourceId, string homeUrl = "https://bato.to") =>
        new(sourceId, "Bato.to (EN)", homeUrl, "/suwayomi/api/v1/extension/icon/bato.png", "en", false);

    [Fact]
    public void IdentifierIsDeterministic()
    {
        Assert.Equal(SuwayomiSource.IdentifierFor(BatoSourceId), SuwayomiSource.IdentifierFor(BatoSourceId));
    }

    [Fact]
    public void IdentifierDiffersPerSource()
    {
        Assert.NotEqual(SuwayomiSource.IdentifierFor(BatoSourceId), SuwayomiSource.IdentifierFor(ComickSourceId));
    }

    [Fact]
    public void IdentifierIsNotEmpty()
    {
        Guid identifier = SuwayomiSource.IdentifierFor(BatoSourceId);
        Assert.NotEqual(Guid.Empty, identifier);
        Assert.NotEqual(Guid.AllBitsSet, identifier);
    }

    [Fact]
    public void IdentifierIsAVersion5Uuid()
    {
        // The version and variant bits are what make this a UUID rather than an arbitrary hash, and downstream code
        // treats these values as ordinary Guids.
        byte[] bytes = SuwayomiSource.IdentifierFor(BatoSourceId).ToByteArray(bigEndian: true);
        Assert.Equal(0x50, bytes[6] & 0xF0);
        Assert.Equal(0x80, bytes[8] & 0xC0);
    }

    [Fact]
    public void ConstructorDerivesIdentifierFromSourceId()
    {
        Assert.Equal(SuwayomiSource.IdentifierFor(BatoSourceId), CreateSource().Identifier);
    }

    [Fact]
    public void ParseIdentifierFromUrlReturnsSourceRelativePath()
    {
        Assert.Equal("/series/12345", CreateSource().ParseIdentifierFromUrl("https://bato.to/series/12345"));
    }

    [Fact]
    public void ParseIdentifierFromUrlKeepsQueryString()
    {
        // Some Tachiyomi sources address a series by query parameter, and Suwayomi stores that whole string as the url.
        Assert.Equal("/manga?id=7", CreateSource().ParseIdentifierFromUrl("https://bato.to/manga?id=7"));
    }

    [Fact]
    public void ParseIdentifierFromUrlRejectsForeignHost()
    {
        Assert.Null(CreateSource().ParseIdentifierFromUrl("https://mangadex.org/title/abc"));
    }

    [Fact]
    public void ParseIdentifierFromUrlRejectsBareHost()
    {
        Assert.Null(CreateSource().ParseIdentifierFromUrl("https://bato.to/"));
    }

    [Fact]
    public void ParseIdentifierFromUrlRejectsNonUrl()
    {
        Assert.Null(CreateSource().ParseIdentifierFromUrl("not-a-url"));
    }

    [Fact]
    public void ParseIdentifierFromUrlReturnsNullWithoutHomeUrl()
    {
        // Suwayomi reports no homeUrl for some sources; those can still be searched, just not linked by pasted url.
        Assert.Null(CreateSource(homeUrl: string.Empty).ParseIdentifierFromUrl("https://bato.to/series/12345"));
    }

    [Fact]
    public void SupportedLanguagesParsesTachiyomiCode()
    {
        SuwayomiSource source = new(BatoSourceId, "Bato.to", "https://bato.to", string.Empty, "pt-BR", false);
        Assert.Single(source.SupportedLanguages);
        Assert.Equal("pt-BR", source.SupportedLanguages[0].Name);
    }

    [Theory]
    [InlineData("all")]
    [InlineData("other")]
    [InlineData("localsourcelang")]
    public void SupportedLanguagesIsEmptyForPseudoCodes(string lang)
    {
        // Tachiyomi's multi-language and unclassified markers have no CultureInfo equivalent; claiming a specific
        // language for them would be wrong, so the list is left empty.
        SuwayomiSource source = new(BatoSourceId, "Bato.to", "https://bato.to", string.Empty, lang, false);
        Assert.Empty(source.SupportedLanguages);
    }
}
