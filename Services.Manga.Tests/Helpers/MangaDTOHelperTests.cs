using Services.Manga.Database;
using Services.Manga.Helpers;
using MangaDto = Services.Manga.Entities.Manga;

namespace Services.Manga.Tests.Helpers;

public class MangaDTOHelperTests
{
    [Fact]
    public void ToDTO_ConvertsDbMangaMetadataEntriesToMangaDTO()
    {
        DbManga manga = new() { MangaId = Guid.NewGuid(), Monitored = true };
        DbMetadata metadata = new()
        {
            MetadataId = Guid.NewGuid(),
            MetadataExtension = Guid.NewGuid(),
            Identifier = "id",
            Series = "Series"
        };
        DbMangaMetadataEntries entry = new()
        {
            MangaId = manga.MangaId,
            MetadataId = metadata.MetadataId,
            Chosen = true,
            Manga = manga,
            Metadata = metadata
        };

        MangaDto dto = entry.ToDTO();

        Assert.Equal(manga.MangaId, dto.MangaId);
        Assert.True(dto.Monitored);
        Assert.NotNull(dto.MetadataEntry);
        Assert.Equal(metadata.MetadataId, dto.MetadataEntry!.MetadataId);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ToDTO_SetsChosenFlagFromEntryOnMetadataEntry(bool chosen)
    {
        DbManga manga = new() { MangaId = Guid.NewGuid(), Monitored = false };
        DbMetadata metadata = new() { MetadataId = Guid.NewGuid(), MetadataExtension = Guid.NewGuid(), Identifier = "id", Series = "Series" };
        DbMangaMetadataEntries entry = new()
        {
            MangaId = manga.MangaId,
            MetadataId = metadata.MetadataId,
            Chosen = chosen,
            Manga = manga,
            Metadata = metadata
        };

        MangaDto dto = entry.ToDTO();

        Assert.Equal(chosen, dto.MetadataEntry!.Chosen);
    }

    [Fact]
    public void ToDTO_PreservesMonitoredStatusFromManga()
    {
        DbManga manga = new() { MangaId = Guid.NewGuid(), Monitored = true };
        DbMetadata metadata = new() { MetadataId = Guid.NewGuid(), MetadataExtension = Guid.NewGuid(), Identifier = "id", Series = "Series" };
        DbMangaMetadataEntries entry = new() { MangaId = manga.MangaId, MetadataId = metadata.MetadataId, Chosen = true, Manga = manga, Metadata = metadata };

        MangaDto dto = entry.ToDTO();

        Assert.True(dto.Monitored);
    }
}
