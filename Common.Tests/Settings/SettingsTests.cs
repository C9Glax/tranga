using Common.Datatypes;

namespace Common.Tests.Settings;

// Settings holds static mutable state shared across the whole test process, so each test
// restores the value it changed to avoid leaking state into other tests.
public class SettingsTests
{
    [Fact]
    public void AllowNSFWCanBeUpdatedAfterInitialization()
    {
        bool original = Common.Settings.Settings.AllowNSFW;
        try
        {
            Common.Settings.Settings.AllowNSFW = !original;
            Assert.Equal(!original, Common.Settings.Settings.AllowNSFW);

            Common.Settings.Settings.AllowNSFW = original;
            Assert.Equal(original, Common.Settings.Settings.AllowNSFW);
        }
        finally
        {
            Common.Settings.Settings.AllowNSFW = original;
        }
    }

    [Fact]
    public void DownloadLanguageCanBeUpdatedAfterInitialization()
    {
        Language original = Common.Settings.Settings.DownloadLanguage;
        try
        {
            Common.Settings.Settings.DownloadLanguage = new Language("ja");
            Assert.Equal("ja", Common.Settings.Settings.DownloadLanguage.Name);

            Common.Settings.Settings.DownloadLanguage = new Language("de");
            Assert.Equal("de", Common.Settings.Settings.DownloadLanguage.Name);
        }
        finally
        {
            Common.Settings.Settings.DownloadLanguage = original;
        }
    }

    [Fact]
    public void ChapterNamingSchemeCanBeUpdatedAfterInitialization()
    {
        string original = Common.Settings.Settings.ChapterNamingScheme;
        try
        {
            Common.Settings.Settings.ChapterNamingScheme = "%C - %T";
            Assert.Equal("%C - %T", Common.Settings.Settings.ChapterNamingScheme);
        }
        finally
        {
            Common.Settings.Settings.ChapterNamingScheme = original;
        }
    }
}