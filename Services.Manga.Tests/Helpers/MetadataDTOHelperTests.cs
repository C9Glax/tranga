using Common.Datatypes;
using Services.Manga.Database;
using Services.Manga.Entities;
using Services.Manga.Helpers;

namespace Services.Manga.Tests.Helpers;

public class MetadataDTOHelperTests
{
    [Fact]
    public void ToDTO_ConvertsDbMetadataToMetadataDTO()
    {
        Guid coverId = Guid.NewGuid();
        DbMetadata metadata = new()
        {
            MetadataId = Guid.NewGuid(),
            MetadataExtension = Guid.NewGuid(),
            Identifier = "id",
            Series = "Series",
            Summary = "Summary",
            Year = 1999,
            Language = "en",
            ChaptersNumber = 42,
            Status = ReleaseStatus.Ongoing,
            CoverId = coverId,
            Url = "https://example.com",
            NSFW = false
        };

        Metadata dto = metadata.ToDTO();

        Assert.Equal(metadata.MetadataId, dto.MetadataId);
        Assert.Equal(metadata.MetadataExtension, dto.MetadataExtensionId);
        Assert.Equal("id", dto.Identifier);
        Assert.Equal("Series", dto.Series);
        Assert.Equal("Summary", dto.Summary);
        Assert.Equal(1999, dto.Year);
        Assert.Equal("en", dto.Language);
        Assert.Equal(42, dto.ChaptersNumber);
        Assert.Equal(ReleaseStatus.Ongoing, dto.Status);
        Assert.Equal(coverId, dto.CoverId);
        Assert.Equal("https://example.com", dto.Url);
        Assert.False(dto.NSFW);
    }

    [Fact]
    public void ToDTO_HandlesNullOptionalFields()
    {
        DbMetadata metadata = new()
        {
            MetadataId = Guid.NewGuid(),
            MetadataExtension = Guid.NewGuid(),
            Identifier = "id",
            Series = "Series",
            Summary = null,
            Year = null,
            Language = null,
            ChaptersNumber = null,
            Status = null,
            CoverId = null,
            Url = null,
            NSFW = null
        };

        Metadata dto = metadata.ToDTO();

        Assert.Null(dto.Summary);
        Assert.Null(dto.Year);
        Assert.Null(dto.Language);
        Assert.Null(dto.ChaptersNumber);
        Assert.Null(dto.Status);
        Assert.Null(dto.CoverId);
        Assert.Null(dto.Url);
        Assert.Null(dto.NSFW);
    }
}
