using System.Text.Json.Serialization;

namespace Common.Services;

/// <summary>Shared <see cref="WebApplicationBuilder"/> setup helpers used by every Tranga service.</summary>
public static class ExtensionMethods
{
    /// <summary>Configures OpenAPI (with enums serialized as strings) and CORS on the builder.</summary>
    /// <param name="builder">The builder to configure.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static WebApplicationBuilder SetupWebApplicationBuilder(this WebApplicationBuilder builder)
    {
        builder.Services.AddOpenApi().ConfigureHttpJsonOptions(opts =>
        {
            opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        builder.Services.AddCors();

        return builder;
    }
}