using Common.Datatypes;

namespace Common.Tests.DataTypes;

public class LanguageTests
{
    [Theory]
    [InlineData("en")]
    [InlineData("en-US")]
    [InlineData("ja")]
    public void CanBeConstructedFromValidCultureName(string cultureName)
    {
        Language language = new(cultureName);

        Assert.Equal(cultureName, language.Name);
    }

    [Fact]
    public void InheritsCultureInfoProperties()
    {
        Language language = new("en-US");

        Assert.Equal("English (United States)", language.EnglishName);
        Assert.Equal("en", language.TwoLetterISOLanguageName);
    }

    [Fact]
    public void ToStringReturnsCultureName()
    {
        Language language = new("en-us");

        Assert.Equal(language.Name, language.ToString());
        Assert.Equal("en-US", language.ToString());
    }

    [Fact]
    public void NamePropertyIsAccessible()
    {
        Language language = new("ja");

        Assert.Equal("ja", language.Name);
    }

    [Fact]
    public void ImplicitConversionToString()
    {
        string? s = new Language("en-us");
        Assert.Equal("en-US", s);
    }

    [Fact]
    public void ImplicitConversionToStringReturnsNullForNullLanguage()
    {
        Language? language = null;
        string? s = language;

        Assert.Null(s);
    }

    [Fact]
    public void ImplicitConversionFromString()
    {
        Language l = "en-us"!;
        Assert.Equal("en-US", l.Name);
    }

    [Fact]
    public void ImplicitConversionFromStringReturnsNullForNullString()
    {
        string? s = null;
        Language? language = s;

        Assert.Null(language);
    }
}