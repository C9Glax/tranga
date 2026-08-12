using Common.Services.Authentication;
using Common.Tests;

namespace Common.Services.Tests.Authentication;

public class ApiKeyHasherTests : TrangaTest
{
    [Fact]
    public void GenerateKey_ProducesDistinctKeys()
    {
        string first = ApiKeyHasher.GenerateKey();
        string second = ApiKeyHasher.GenerateKey();

        Assert.NotEqual(first, second);
        Assert.StartsWith("tga_", first);
    }

    [Fact]
    public void Hash_IsDeterministic()
    {
        string key = ApiKeyHasher.GenerateKey();

        Assert.Equal(ApiKeyHasher.Hash(key), ApiKeyHasher.Hash(key));
    }

    [Fact]
    public void Hash_DiffersBetweenDifferentKeys()
    {
        string first = ApiKeyHasher.GenerateKey();
        string second = ApiKeyHasher.GenerateKey();

        Assert.NotEqual(ApiKeyHasher.Hash(first), ApiKeyHasher.Hash(second));
    }
}
