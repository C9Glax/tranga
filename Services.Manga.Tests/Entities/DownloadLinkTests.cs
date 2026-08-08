using System.ComponentModel.DataAnnotations;
using Services.Manga.Entities;

namespace Services.Manga.Tests.Entities;

public class DownloadLinkTests
{
    private static DownloadLink CreateValid() => new()
    {
        DownloadId = Guid.NewGuid(),
        DownloadExtensionId = Guid.NewGuid(),
        Identifier = "identifier",
        Series = "Series",
        Summary = "Summary",
        Language = "en",
        Url = "https://example.com",
        CoverId = Guid.NewGuid(),
        NSFW = false
    };

    private static bool TryValidate(DownloadLink link, out List<ValidationResult> results)
    {
        results = [];
        return Validator.TryValidateObject(link, new ValidationContext(link), results, validateAllProperties: true);
    }

    [Fact]
    public void CanBeConstructedWithRequiredFields()
    {
        Guid downloadId = Guid.NewGuid();
        Guid extensionId = Guid.NewGuid();

        DownloadLink link = new()
        {
            DownloadId = downloadId,
            DownloadExtensionId = extensionId,
            Identifier = "identifier",
            Series = "Series",
            Summary = null,
            Url = null,
            CoverId = null,
            NSFW = null
        };

        Assert.Equal(downloadId, link.DownloadId);
        Assert.Equal(extensionId, link.DownloadExtensionId);
        Assert.Equal("identifier", link.Identifier);
        Assert.Equal("Series", link.Series);
        Assert.Null(link.Summary);
        Assert.Null(link.Language);
        Assert.Null(link.Url);
        Assert.Null(link.CoverId);
        Assert.Null(link.NSFW);
    }

    [Fact]
    public void SeriesAtMaxLengthPassesValidation()
    {
        DownloadLink link = CreateValid() with { Series = new string('a', 1024) };

        Assert.True(TryValidate(link, out _));
    }

    [Fact]
    public void SeriesExceedingMaxLengthFailsValidation()
    {
        DownloadLink link = CreateValid() with { Series = new string('a', 1025) };

        Assert.False(TryValidate(link, out List<ValidationResult> results));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(DownloadLink.Series)));
    }

    [Fact]
    public void SummaryIsOptionalAndAcceptsUpToMaxLength()
    {
        DownloadLink withoutSummary = CreateValid() with { Summary = null };
        DownloadLink atLimit = CreateValid() with { Summary = new string('a', 4096) };

        Assert.True(TryValidate(withoutSummary, out _));
        Assert.True(TryValidate(atLimit, out _));
    }

    [Fact]
    public void SummaryExceedingMaxLengthFailsValidation()
    {
        DownloadLink link = CreateValid() with { Summary = new string('a', 4097) };

        Assert.False(TryValidate(link, out List<ValidationResult> results));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(DownloadLink.Summary)));
    }

    [Fact]
    public void LanguageIsOptionalAndAcceptsUpToMaxLength()
    {
        DownloadLink withoutLanguage = CreateValid() with { Language = null };
        DownloadLink atLimit = CreateValid() with { Language = "12345678" };

        Assert.True(TryValidate(withoutLanguage, out _));
        Assert.True(TryValidate(atLimit, out _));
    }

    [Fact]
    public void LanguageExceedingMaxLengthFailsValidation()
    {
        DownloadLink link = CreateValid() with { Language = "123456789" };

        Assert.False(TryValidate(link, out List<ValidationResult> results));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(DownloadLink.Language)));
    }

    [Fact]
    public void IsARecordWithValueEquality()
    {
        DownloadLink a = CreateValid();
        DownloadLink b = a with { };

        Assert.Equal(a, b);
    }
}
