using Common.Services.Authentication;
using Common.Tests;

namespace Common.Services.Tests.Authentication;

public class PasswordHasherTests : TrangaTest
{
    [Fact]
    public void Hash_ThenVerify_WithCorrectPassword_Succeeds()
    {
        string hash = PasswordHasher.Hash("correct-password");

        Assert.True(PasswordHasher.Verify("correct-password", hash));
    }

    [Fact]
    public void Verify_WithWrongPassword_Fails()
    {
        string hash = PasswordHasher.Hash("correct-password");

        Assert.False(PasswordHasher.Verify("wrong-password", hash));
    }

    [Fact]
    public void Hash_IsSaltedDifferentlyEachTime()
    {
        string first = PasswordHasher.Hash("same-password");
        string second = PasswordHasher.Hash("same-password");

        Assert.NotEqual(first, second);
        Assert.True(PasswordHasher.Verify("same-password", first));
        Assert.True(PasswordHasher.Verify("same-password", second));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-valid-hash")]
    [InlineData("1.2")]
    public void Verify_WithMalformedStoredHash_FailsInsteadOfThrowing(string stored)
    {
        Assert.False(PasswordHasher.Verify("anything", stored));
    }
}
