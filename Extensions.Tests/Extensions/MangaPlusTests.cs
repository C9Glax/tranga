using Common.Datatypes;
using Extensions.Data;
using Extensions.Extensions;
using Extensions.Extensions.MangaPlusApi;

namespace Extensions.Tests.Extensions;

public sealed class MangaPlusTests : DownloadExtensionTests<MangaPlus>
{
    private const string OnePieceUrl = "https://mangaplus.shueisha.co.jp/titles/100020";

    #region Offline

    [Fact]
    public void XorRoundtripsWithMultiByteKey()
    {
        byte[] original = Enumerable.Range(0, 1000).Select(i => (byte)(i * 7 % 255)).ToArray();
        byte[] data = [.. original];
        byte[] key = [0xDE, 0xAD, 0xBE, 0xEF, 0x01];

        MangaPlus.Xor(data, key);

        Assert.NotEqual(original, data);
        Assert.Equal((byte)(original[0] ^ 0xDE), data[0]);
        Assert.Equal((byte)(original[5] ^ 0xDE), data[5]); // the key repeats
        MangaPlus.Xor(data, key);
        Assert.Equal(original, data);
    }

    [Fact]
    public void XorWithEmptyKeyLeavesDataUntouched()
    {
        byte[] data = [1, 2, 3];
        MangaPlus.Xor(data, []);
        Assert.Equal(new byte[] { 1, 2, 3 }, data);
    }

    [Theory]
    [InlineData("#12", "12")]
    [InlineData("#012", "12")]
    [InlineData("#001", "1")]
    [InlineData("#12.5", "12.5")]
    [InlineData("#0", "0")]
    [InlineData("ex", null)]
    [InlineData("One-shot", null)]
    [InlineData("#", null)]
    public void ParseChapterNumber(string name, string? expected) =>
        Assert.Equal(expected, MangaPlus.ParseChapterNumber(name));

    [Theory]
    [InlineData("en", "eng", 0)]
    [InlineData("en-US", "eng", 0)]
    [InlineData("es", "esp", 1)]
    [InlineData("pt-BR", "ptb", 4)]
    [InlineData("pt", "ptb", 4)]
    [InlineData("de", "deu", 7)]
    [InlineData("vi", "vie", 9)]
    public void TryGetLanguageMapsSupportedLanguages(string language, string name, int code)
    {
        Assert.True(MangaPlus.TryGetLanguage(language, out MangaPlus.MangaPlusLanguage result));
        Assert.Equal(name, result.Name);
        Assert.Equal(code, result.Code);
    }

    [Theory]
    [InlineData("ja")]
    [InlineData("it")]
    public void TryGetLanguageRejectsUnsupportedLanguages(string language) =>
        Assert.False(MangaPlus.TryGetLanguage(language, out _));

    [Fact]
    public void TryGetLanguageRejectsNull() => Assert.False(MangaPlus.TryGetLanguage(null, out _));

    [Theory]
    [InlineData("https://mangaplus.shueisha.co.jp/titles/100020", "100020")]
    [InlineData("https://mangaplus.shueisha.co.jp/titles/100020/", "100020")]
    [InlineData("https://www.mangaplus.shueisha.co.jp/titles/100020", "100020")]
    public void ParseIdentifierFromUrlParsesTitleUrls(string url, string expected) =>
        Assert.Equal(expected, _extension.ParseIdentifierFromUrl(url));

    [Theory]
    [InlineData("https://mangaplus.shueisha.co.jp/viewer/1000486")]
    [InlineData("https://mangaplus.shueisha.co.jp/titles/not-a-number")]
    [InlineData("https://mangaplus.shueisha.co.jp/titles/-5")]
    [InlineData("https://example.com/titles/100020")]
    [InlineData("not a url")]
    public void ParseIdentifierFromUrlRejectsOtherUrls(string url) => Assert.Null(_extension.ParseIdentifierFromUrl(url));

    [Fact]
    public void ParseChaptersSkipsExpiredAndNumberlessChaptersAndSortsByDate()
    {
        TitleDetailView detail = new()
        {
            Title = new Title { TitleId = 1, Name = "Test", Language = 0 },
            ChapterListGroup =
            {
                new ChapterListGroup
                {
                    FirstChapterList =
                    {
                        new Chapter { ChapterId = 30, Name = "#003", SubTitle = "Third", StartTimeStamp = 3000 },
                        new Chapter { ChapterId = 10, Name = "#001", SubTitle = "First", StartTimeStamp = 1000 },
                        new Chapter { ChapterId = 11, Name = "#002", StartTimeStamp = 2000 }, // expired: no subtitle
                    },
                    LastChapterList =
                    {
                        new Chapter { ChapterId = 40, Name = "ex", SubTitle = "Extra", StartTimeStamp = 4000 },
                        new Chapter { ChapterId = 30, Name = "#003", SubTitle = "Third", StartTimeStamp = 3000 }, // duplicate
                    }
                }
            }
        };

        List<ChapterInfo> chapters = _extension.ParseChapters(detail);

        Assert.Collection(chapters,
            first =>
            {
                Assert.Equal("1", first.Number);
                Assert.Equal("10", first.Identifier);
                Assert.Equal("First", first.Title);
                Assert.Equal("https://mangaplus.shueisha.co.jp/viewer/10", first.Url);
                Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1000), first.ReleaseDate);
                Assert.Equal(_extension.Identifier, first.ExtensionIdentifier);
            },
            third =>
            {
                Assert.Equal("3", third.Number);
                Assert.Equal("30", third.Identifier);
            });
    }

    [Fact]
    public async Task SearchWithUnsupportedLanguageReturnsEmpty()
    {
        List<MangaInfo>? result = await _extension.SearchDownload(new SearchQuery { Title = "One Piece", Language = "ja" }, ct);
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchWithoutTitleReturnsEmpty()
    {
        List<MangaInfo>? result = await _extension.SearchDownload(new SearchQuery { Language = "en" }, ct);
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region Live

    [Fact]
    public async Task SearchFindsOnePiece()
    {
        List<MangaInfo>? result = await _extension.SearchDownload(new SearchQuery { Title = "one piece", Language = "en" }, ct);

        Assert.NotNull(result);
        MangaInfo manga = Assert.Single(result, m => m.Identifier == "100020");
        Assert.Equal(OnePieceUrl, manga.Url);
        Assert.Equal(_extension.Identifier, manga.ExtensionIdentifier);
        Assert.True(manga.Cover.Length > 0);
        Assert.False(manga.NSFW);
    }

    [Fact]
    public async Task ChaptersAreListedAndImagesDecryptToValidImages()
    {
        MangaInfo manga = new(_extension.Identifier, "ONE PIECE", OnePieceUrl, "100020", new Common.Helpers.TrangaImage());

        List<ChapterInfo>? chapters = await _extension.GetChapters(manga, ct);

        Assert.NotNull(chapters);
        Assert.NotEmpty(chapters);
        Assert.All(chapters, c => Assert.False(string.IsNullOrEmpty(c.Number)));

        // GetChapterImages re-encodes every page to JPEG and returns null when one cannot be decoded, so a wrong or
        // missing XOR decryption would show up here.
        List<ChapterImage>? images = await ((IDownloadExtension)_extension).GetChapterImages(chapters.First(), ct);

        Assert.NotNull(images);
        Assert.NotEmpty(images);
        Assert.Equal(Enumerable.Range(0, images.Count), images.Select(i => i.order));
        Assert.All(images, i => Assert.True(i.image.Length > 0));
    }

    [Fact]
    public async Task ChaptersOfUnknownTitleReturnNull()
    {
        MangaInfo manga = new(_extension.Identifier, "?", "https://mangaplus.shueisha.co.jp/titles/1", "1", new Common.Helpers.TrangaImage());
        Assert.Null(await _extension.GetChapters(manga, ct));
    }

    #endregion
}
