using System.ComponentModel.DataAnnotations;
using Services.Manga.Entities;

namespace Services.Manga.Tests.Entities;

public class ChapterTests
{
    private static Chapter CreateValid() => new()
    {
        ChapterId = Guid.NewGuid(),
        MangaId = Guid.NewGuid(),
        Title = "Chapter Title",
        Volume = "1",
        Number = "1",
        ReleaseDate = DateTimeOffset.UtcNow
    };

    private static bool TryValidate(Chapter chapter, out List<ValidationResult> results)
    {
        results = [];
        return Validator.TryValidateObject(chapter, new ValidationContext(chapter), results, validateAllProperties: true);
    }

    [Fact]
    public void CanBeConstructedWithChapterIdMangaIdAndNumber()
    {
        Guid chapterId = Guid.NewGuid();
        Guid mangaId = Guid.NewGuid();

        Chapter chapter = new()
        {
            ChapterId = chapterId,
            MangaId = mangaId,
            Title = null,
            Volume = null,
            Number = "12",
            ReleaseDate = null
        };

        Assert.Equal(chapterId, chapter.ChapterId);
        Assert.Equal(mangaId, chapter.MangaId);
        Assert.Null(chapter.Title);
        Assert.Null(chapter.Volume);
        Assert.Equal("12", chapter.Number);
        Assert.Null(chapter.ReleaseDate);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("1.5")]
    [InlineData("Special")]
    public void VolumeAcceptsVariousFormats(string volume)
    {
        Chapter chapter = CreateValid() with { Volume = volume };

        Assert.Equal(volume, chapter.Volume);
        Assert.True(TryValidate(chapter, out _));
    }

    [Fact]
    public void TitleAtMaxLengthPassesValidation()
    {
        Chapter chapter = CreateValid() with { Title = new string('a', 2048) };

        Assert.True(TryValidate(chapter, out _));
    }

    [Fact]
    public void TitleExceedingMaxLengthFailsValidation()
    {
        Chapter chapter = CreateValid() with { Title = new string('a', 2049) };

        Assert.False(TryValidate(chapter, out List<ValidationResult> results));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Chapter.Title)));
    }

    [Fact]
    public void VolumeAtMaxLengthPassesValidation()
    {
        Chapter chapter = CreateValid() with { Volume = new string('1', 16) };

        Assert.True(TryValidate(chapter, out _));
    }

    [Fact]
    public void VolumeExceedingMaxLengthFailsValidation()
    {
        Chapter chapter = CreateValid() with { Volume = new string('1', 17) };

        Assert.False(TryValidate(chapter, out List<ValidationResult> results));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Chapter.Volume)));
    }

    [Fact]
    public void NumberAtMaxLengthPassesValidation()
    {
        Chapter chapter = CreateValid() with { Number = new string('1', 16) };

        Assert.True(TryValidate(chapter, out _));
    }

    [Fact]
    public void NumberExceedingMaxLengthFailsValidation()
    {
        Chapter chapter = CreateValid() with { Number = new string('1', 17) };

        Assert.False(TryValidate(chapter, out List<ValidationResult> results));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Chapter.Number)));
    }

    [Fact]
    public void ReleaseDateIsNullable()
    {
        Chapter chapter = CreateValid() with { ReleaseDate = null };

        Assert.Null(chapter.ReleaseDate);
        Assert.True(TryValidate(chapter, out _));
    }

    [Fact]
    public void IsARecordWithValueEquality()
    {
        Chapter a = CreateValid();
        Chapter b = a with { };

        Assert.Equal(a, b);
    }
}
