using System.ComponentModel.DataAnnotations;
using Common.Datatypes;
using Services.Manga.Entities;

namespace Services.Manga.Tests.Entities;

public class MetadataTests
{
    private static Metadata CreateValid() => new()
    {
        MetadataId = Guid.NewGuid(),
        MetadataExtensionId = Guid.NewGuid(),
        Identifier = "identifier",
        Series = "Series",
        Summary = "Summary",
        Year = 1999,
        Language = "en",
        ChaptersNumber = 42,
        CoverId = Guid.NewGuid(),
        Genres = ["Action"],
        Authors = ["Author"],
        Artists = ["Artist"],
        Url = "https://example.com",
        Status = ReleaseStatus.Ongoing,
        NSFW = false
    };

    private static bool TryValidate(Metadata metadata, out List<ValidationResult> results)
    {
        results = [];
        return Validator.TryValidateObject(metadata, new ValidationContext(metadata), results, validateAllProperties: true);
    }

    [Fact]
    public void CanBeConstructedWithRequiredFields()
    {
        Guid metadataId = Guid.NewGuid();
        Guid extensionId = Guid.NewGuid();

        Metadata metadata = new()
        {
            MetadataId = metadataId,
            MetadataExtensionId = extensionId,
            Identifier = "identifier",
            Series = "Series",
            Summary = null,
            CoverId = null,
            Url = null,
            NSFW = null
        };

        Assert.Equal(metadataId, metadata.MetadataId);
        Assert.Equal(extensionId, metadata.MetadataExtensionId);
        Assert.Equal("identifier", metadata.Identifier);
        Assert.Equal("Series", metadata.Series);
        Assert.Null(metadata.Summary);
        Assert.Null(metadata.CoverId);
        Assert.Null(metadata.Url);
        Assert.Null(metadata.NSFW);
        Assert.Null(metadata.Chosen);
        Assert.Null(metadata.Year);
        Assert.Null(metadata.ChaptersNumber);
        Assert.Null(metadata.Status);
    }

    [Fact]
    public void SeriesAtMaxLengthPassesValidation()
    {
        Metadata metadata = CreateValid() with { Series = new string('a', 1024) };

        Assert.True(TryValidate(metadata, out _));
    }

    [Fact]
    public void SeriesExceedingMaxLengthFailsValidation()
    {
        Metadata metadata = CreateValid() with { Series = new string('a', 1025) };

        Assert.False(TryValidate(metadata, out List<ValidationResult> results));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Metadata.Series)));
    }

    [Fact]
    public void SummaryIsOptionalAndAcceptsUpToMaxLength()
    {
        Metadata withoutSummary = CreateValid() with { Summary = null };
        Metadata atLimit = CreateValid() with { Summary = new string('a', 4096) };

        Assert.True(TryValidate(withoutSummary, out _));
        Assert.True(TryValidate(atLimit, out _));
    }

    [Fact]
    public void SummaryExceedingMaxLengthFailsValidation()
    {
        Metadata metadata = CreateValid() with { Summary = new string('a', 4097) };

        Assert.False(TryValidate(metadata, out List<ValidationResult> results));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Metadata.Summary)));
    }

    [Fact]
    public void YearChaptersNumberAndLanguageAreNullable()
    {
        Metadata metadata = CreateValid() with { Year = null, ChaptersNumber = null, Language = null };

        Assert.Null(metadata.Year);
        Assert.Null(metadata.ChaptersNumber);
        Assert.Null(metadata.Language);
    }

    [Fact]
    public void GenresAuthorsArtistsCanBeEmptyArrays()
    {
        Metadata metadata = CreateValid() with { Genres = [], Authors = [], Artists = [] };

        Assert.Empty(metadata.Genres);
        Assert.Empty(metadata.Authors);
        Assert.Empty(metadata.Artists);
    }

    [Fact]
    public void StatusAndNsfwAreNullableBooleansOrEnums()
    {
        Metadata metadata = CreateValid() with { Status = null, NSFW = null };

        Assert.Null(metadata.Status);
        Assert.Null(metadata.NSFW);
    }

    [Fact]
    public void IsARecordWithValueEquality()
    {
        Metadata a = CreateValid();
        Metadata b = a with { };

        Assert.Equal(a, b);
    }
}
