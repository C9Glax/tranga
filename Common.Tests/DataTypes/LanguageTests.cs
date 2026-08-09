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

    [Theory]
    [InlineData("en", "en")]
    [InlineData("en", "en-US")]
    [InlineData("en", "en-GB")]
    [InlineData("en-US", "en")]
    [InlineData("en-US", "en-us")]
    [InlineData("en-US", "EN-US")]
    public void EqualsStringTreatsBareLanguageAsMatchingAnySubLocale(string language, string other)
    {
        Language l = new(language);

        Assert.True(l.Equals(other));
    }

    [Theory]
    [InlineData("en-US", "en-GB")]
    [InlineData("en-GB", "en-US")]
    [InlineData("en", "de")]
    [InlineData("en-US", "de-AT")]
    public void EqualsStringReturnsFalseForDifferentSubLocalesOrLanguages(string language, string other)
    {
        Language l = new(language);

        Assert.False(l.Equals(other));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-real-culture-code")]
    public void EqualsStringReturnsFalseForUnparseableOrEmptyInput(string? other)
    {
        Language l = new("en");

        Assert.False(l.Equals(other));
    }

    [Fact]
    public void EqualsLanguageTreatsBareLanguageAsMatchingAnySubLocale()
    {
        Language bare = new("en");
        Language subLocale = new("en-US");

        Assert.True(bare.Equals(subLocale));
        Assert.True(subLocale.Equals(bare));
    }

    [Fact]
    public void EqualsLanguageReturnsFalseForNull()
    {
        Language l = new("en");

        Assert.False(l.Equals((Language?)null));
    }
}