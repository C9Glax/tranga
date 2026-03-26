using Common.Datatypes;

namespace Common.Datatypes.Tests;

public class LanguageTests
{
    [Fact]
    public void ImplicitConversionFromString()
    {
        Language l = "en-us"!;
        Assert.Equal("en-US", l.Name);
    }
    
    [Fact]
    public void ImplicitConversionToString()
    {
        string? s = new Language("en-us");
        Assert.Equal("en-US", s);
    }
}