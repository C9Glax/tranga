using Common.Settings;
using Services.Manga.Database;
using Services.Tasks.Helpers;

namespace Services.Tasks.Tests.Helpers;

public class ChapterFileHelperTests
{
    [Fact]
    public void ParametersReplaced()
    {
        DbChapter chapter = new ()
        {
            MangaId = Guid.Empty,
            Volume = "1",
            Number = "2",
            Title = "3"
        };
        Settings.ChapterNamingScheme = "%V %C %T";

        string filename = chapter.CreateFileName();
        Assert.Equal("1 2 3.cbz", filename);
    }
    
    [Theory]
    [CombinatorialData]
    public void OptionalParametersNotIncluded([CombinatorialValues("V", "T")]string parameter)
    {
        DbChapter chapter = new ()
        {
            MangaId = Guid.Empty,
            Number = "2",
        };
        Settings.ChapterNamingScheme = $"?{parameter}(%{parameter})";

        string filename = chapter.CreateFileName();
        Assert.Equal(".cbz", filename);
    }
    
    [Fact]
    public void OptionalParametersDoNotSwallowFollowingTokens()
    {
        DbChapter chapter = new ()
        {
            MangaId = Guid.Empty,
            Volume = null,
            Number = "2",
            Title = null
        };
        Settings.ChapterNamingScheme = "?V(Vol. %V )Ch. %C?T( - %T)";

        string filename = chapter.CreateFileName();
        Assert.Equal("Ch. 2.cbz", filename);
    }

    [Fact]
    public void OptionalParametersIncludedWhenPresent()
    {
        DbChapter chapter = new ()
        {
            MangaId = Guid.Empty,
            Volume = "1",
            Number = "2",
            Title = "Title"
        };
        Settings.ChapterNamingScheme = "?V(Vol. %V )Ch. %C?T( - %T)";

        string filename = chapter.CreateFileName();
        Assert.Equal("Vol. 1 Ch. 2 - Title.cbz", filename);
    }

    [Theory]
    [CombinatorialData]
    public void UnsafeCharactersFiltered([CombinatorialValues('#', '$', '%', '&', '*', '<', '>', ':', '"', '/', '\\', '|', '?')]char character)
    {
        DbChapter chapter = new ()
        {
            MangaId = Guid.Empty,
            Number = $"{character}",
        };
        Settings.ChapterNamingScheme = "%C";

        string filename = chapter.CreateFileName();
        Assert.Equal(".cbz", filename);
    }
}