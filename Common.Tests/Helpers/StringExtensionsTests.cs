using Common.Helpers;

namespace Common.Tests.Helpers;

public class StringExtensionsTests
{
    [Theory]
    [CombinatorialData]
    public void SafeFilesystemString_StripsUnsafeCharacters([CombinatorialValues('#', '$', '%', '&', '*', '<', '>', ':', '"', '/', '\\', '|', '?')]char character)
    {
        string input = $"a{character}b";

        string result = input.SafeFilesystemString();

        Assert.Equal("ab", result);
    }

    [Fact]
    public void SafeFilesystemString_KeepsAlphanumericsDashesDotsUnderscoresAndSpaces()
    {
        string input = "Some Series-Name_v2.1";

        string result = input.SafeFilesystemString();

        Assert.Equal(input, result);
    }
}
