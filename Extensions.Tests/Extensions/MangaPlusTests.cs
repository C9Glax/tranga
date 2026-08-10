using Common.Datatypes;
using Common.Helpers;
using Extensions.Data;
using Extensions.Extensions;

namespace Extensions.Tests.Extensions;

public sealed class MangaPlusTests : DownloadExtensionTests<MangaPlus>
{
    // https://mangaplus.shueisha.co.jp/titles/100020 (One Piece, English)
    private const string OnePieceTitleId = "100020";

    // https://mangaplus.shueisha.co.jp/viewer/1000486 (Chapter 1: Romance Dawn - permanently free)
    private const string Chapter1Id = "1000486";

    [Fact]
    public async Task SearchDownloadReturnsManga()
    {
        SearchQuery searchQuery = new()
        {
            Title = "One Piece"
        };
        List<MangaInfo>? searchResult = await _extension.SearchDownload(searchQuery, ct);
        Assert.NotNull(searchResult);
        MangaInfo? manga = searchResult.FirstOrDefault(r => r.Identifier == OnePieceTitleId);
        Assert.NotNull(manga);
        Assert.Equal("One Piece", manga.Title);
        Assert.Equal("https://mangaplus.shueisha.co.jp/titles/100020", manga.Url);
        Assert.True(manga.Cover.Length > 0);
    }

    [Fact]
    public async Task SearchMetadataReturnsManga()
    {
        SearchQuery searchQuery = new()
        {
            Title = "One Piece"
        };
        List<SearchResult>? searchResult = await _extension.SearchMetadata(searchQuery, ct);
        Assert.NotNull(searchResult);
        SearchResult? manga = searchResult.FirstOrDefault(r => r.Identifier == OnePieceTitleId);
        Assert.NotNull(manga);
        Assert.Equal("One Piece", manga.Series);
        Assert.True(manga.Cover.Length > 0);
    }

    [Fact]
    public async Task ChapterRetrievalReturnsChaptersAndExcludesExpiredOnes()
    {
        MangaInfo mangaInfo = new(
            _extension.Identifier,
            "One Piece",
            "https://mangaplus.shueisha.co.jp/titles/100020",
            OnePieceTitleId,
            new TrangaImage()
        );
        List<ChapterInfo>? chapters = await _extension.GetChapters(mangaInfo, ct);
        Assert.NotNull(chapters);
        Assert.NotEmpty(chapters);
        ChapterInfo? chapter1 = chapters.FirstOrDefault(c => c.Identifier == Chapter1Id);
        Assert.NotNull(chapter1);
        Assert.Equal("001", chapter1.Number);
        Assert.Equal("Chapter 1: Romance Dawn", chapter1.Title);
        // Only permanently-free/first chapters and the current handful of latest ones stay non-expired;
        // most of a long-running series' back catalog is paywalled and must be excluded.
        Assert.True(chapters.Count < 20);
    }

    [Fact]
    public async Task ChapterImagesReturnsDecryptedImages()
    {
        ChapterInfo chapterInfo = new(
            _extension.Identifier,
            "001",
            "https://mangaplus.shueisha.co.jp/viewer/1000486",
            Chapter1Id
        );
        List<ChapterImage>? images = await _extension.FetchChapterImages(chapterInfo, ct);
        Assert.NotNull(images);
        Assert.NotEmpty(images);
        Assert.All(images, i => Assert.Equal(Chapter1Id, i.chapterIdentifier));

        // The site XOR-encrypts chapter images; a wrong keystream/algorithm would still produce a
        // non-empty stream, so verify the first image actually decrypted to a valid JPEG.
        byte[] firstImageBytes = images.OrderBy(i => i.order).First().image.ToArray();
        Assert.True(firstImageBytes.Length > 4);
        Assert.Equal(0xFF, firstImageBytes[0]);
        Assert.Equal(0xD8, firstImageBytes[1]);
        Assert.Equal(0xFF, firstImageBytes[2]);
    }

    [Fact]
    public void ParseIdentifierFromUrlReturnsTitleId()
    {
        Assert.Equal(OnePieceTitleId, _extension.ParseIdentifierFromUrl("https://mangaplus.shueisha.co.jp/titles/100020"));
    }

    [Fact]
    public void ParseIdentifierFromUrlReturnsNullForNonIntegerSegment()
    {
        Assert.Null(_extension.ParseIdentifierFromUrl("https://mangaplus.shueisha.co.jp/titles/one-piece"));
    }
}
