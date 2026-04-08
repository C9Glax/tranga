using System.Text.Json.Serialization;

namespace Common.Services;

public static class Helpers
{
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