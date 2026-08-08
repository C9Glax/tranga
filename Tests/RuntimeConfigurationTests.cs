using API;
using Microsoft.Extensions.Configuration;

namespace Tests;

public class RuntimeConfigurationTests
{
    [Fact]
    public void GetAllowedCorsOrigin_ReturnsNull_WhenValueIsNotConfigured()
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>());

        string? allowedCorsOrigin = RuntimeConfiguration.GetAllowedCorsOrigin(configuration);

        Assert.Null(allowedCorsOrigin);
    }

    [Fact]
    public void GetAllowedCorsOrigin_ReturnsConfiguredOrigin()
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["TRANGA_CORS:ALLOWED_ORIGIN"] = "https://tranga.example.test"
        });

        string? allowedCorsOrigin = RuntimeConfiguration.GetAllowedCorsOrigin(configuration);

        Assert.Equal("https://tranga.example.test", allowedCorsOrigin);
    }

    [Fact]
    public void IsSwaggerEnabled_ReturnsTrue_WhenValueIsNotConfigured()
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>());

        bool swaggerEnabled = RuntimeConfiguration.IsSwaggerEnabled(configuration);

        Assert.True(swaggerEnabled);
    }

    [Fact]
    public void IsSwaggerEnabled_ReturnsFalse_WhenConfiguredFalse()
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["TRANGA_SWAGGER:ENABLED"] = "false"
        });

        bool swaggerEnabled = RuntimeConfiguration.IsSwaggerEnabled(configuration);

        Assert.False(swaggerEnabled);
    }

    private static IConfiguration CreateConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
