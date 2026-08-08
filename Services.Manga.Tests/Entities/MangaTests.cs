using Services.Manga.Entities;
using MangaDto = Services.Manga.Entities.Manga;

namespace Services.Manga.Tests.Entities;

public class MangaTests
{
    [Fact]
    public void CanBeConstructedWithRequiredFields()
    {
        Guid mangaId = Guid.NewGuid();

        MangaDto manga = new()
        {
            MangaId = mangaId,
            Monitored = true
        };

        Assert.Equal(mangaId, manga.MangaId);
        Assert.True(manga.Monitored);
        Assert.Null(manga.MetadataEntry);
        Assert.Null(manga.DownloadLinks);
    }

    [Fact]
    public void CanOptionallyIncludeMetadataEntryAndDownloadLinks()
    {
        Metadata metadata = new()
        {
            MetadataId = Guid.NewGuid(),
            MetadataExtensionId = Guid.NewGuid(),
            Identifier = "identifier",
            Series = "Series",
            Summary = null,
            CoverId = null,
            Url = null,
            NSFW = false
        };
        DownloadLink downloadLink = new()
        {
            DownloadId = Guid.NewGuid(),
            DownloadExtensionId = Guid.NewGuid(),
            Identifier = "identifier",
            Series = "Series",
            Summary = null,
            Url = null,
            CoverId = null,
            NSFW = false
        };

        MangaDto manga = new()
        {
            MangaId = Guid.NewGuid(),
            Monitored = false,
            MetadataEntry = metadata,
            DownloadLinks = [downloadLink]
        };

        Assert.Equal(metadata, manga.MetadataEntry);
        Assert.NotNull(manga.DownloadLinks);
        Assert.Single(manga.DownloadLinks);
        Assert.Equal(downloadLink, manga.DownloadLinks[0]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MonitoredBooleanIsCorrectlyStored(bool monitored)
    {
        MangaDto manga = new() { MangaId = Guid.NewGuid(), Monitored = monitored };

        Assert.Equal(monitored, manga.Monitored);
    }

    [Fact]
    public void IsARecordWithValueEquality()
    {
        Guid mangaId = Guid.NewGuid();
        MangaDto a = new() { MangaId = mangaId, Monitored = true };
        MangaDto b = new() { MangaId = mangaId, Monitored = true };

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void InstancesWithDifferentValuesAreNotEqual()
    {
        MangaDto a = new() { MangaId = Guid.NewGuid(), Monitored = true };
        MangaDto b = new() { MangaId = Guid.NewGuid(), Monitored = true };

        Assert.NotEqual(a, b);
    }
}
