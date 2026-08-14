namespace API;

public static class RuntimeConfiguration
{
    public static string? GetAllowedCorsOrigin(IConfiguration configuration)
    {
        string? allowedCorsOrigin = configuration["TRANGA_CORS:ALLOWED_ORIGIN"];

        if (string.IsNullOrWhiteSpace(allowedCorsOrigin))
        {
            return null;
        }

        return allowedCorsOrigin;
    }

    public static bool IsSwaggerEnabled(IConfiguration configuration)
    {
        string? configuredValue = configuration["TRANGA_SWAGGER:ENABLED"];

        if (bool.TryParse(configuredValue, out bool swaggerEnabled))
        {
            return swaggerEnabled;
        }

        return true;
    }
}
