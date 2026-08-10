using Common.Services.Events;
using Common.Settings;
using Scalar.AspNetCore;

namespace Common.Services;

/// <summary>
/// Base class for every Tranga service entrypoint. Builds the <see cref="WebApplicationBuilder"/>, wires up CORS/OpenAPI/Scalar
/// and RabbitMQ (skipped during OpenAPI spec generation, see <see cref="Common.Settings.Constants.OpenApiDocumentationRun"/>),
/// and exposes <see cref="SetupWebApplication{TEndpointsBuilder}"/> for the concrete service to register its endpoints.
/// </summary>
public abstract class Service : IAsyncDisposable
{
    /// <summary>The application builder used to configure services before <see cref="App"/> is built.</summary>
    protected WebApplicationBuilder Builder { get; init; }
    /// <summary>The built web application, available after <see cref="SetupWebApplication{TEndpointsBuilder}"/> has run.</summary>
    protected WebApplication App { get; set; }

    /// <summary>
    /// Creates the web application builder, configures RabbitMQ from the <c>RABBITMQ_*</c> environment variables
    /// (unless running in OpenAPI documentation mode), and sets up logging and service defaults.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the service.</param>
    public Service(string[] args)
    {
        Builder = WebApplication.CreateBuilder(args).SetupWebApplicationBuilder();

        if (!Constants.OpenApiDocumentationRun)
        {
            string host = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ??
                          throw new Exception("Missing required EnvVar 'RABBITMQ_HOST'");
            int port = Environment.GetEnvironmentVariable("RABBITMQ_PORT") is { } val
                ? int.Parse(val)
                : throw new Exception("Missing required EnvVar 'RABBITMQ_PORT'");
            string user = Environment.GetEnvironmentVariable("RABBITMQ_USER") ??
                          throw new Exception("Missing required EnvVar 'RABBITMQ_USER'");
            string pass = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ??
                          throw new Exception("Missing required EnvVar 'RABBITMQ_PASSWORD'");

            Builder.Services.AddRabbitMq(host, port, user, pass);
        }
        
        Builder.Logging.ClearProviders();
        Builder.Logging.AddConsole();

        Builder.AddServiceDefaults();
    }

    /// <summary>
    /// Builds the web application, configures permissive CORS, maps Aspire default endpoints, registers the service's
    /// endpoints via a new <typeparamref name="TEndpointsBuilder"/>, and enables HTTPS redirection and OpenAPI/Scalar docs.
    /// </summary>
    /// <typeparam name="TEndpointsBuilder">The concrete <see cref="EndpointsBuilder"/> that registers this service's routes.</typeparam>
    /// <param name="endpointsPrefix">Route prefix all of the service's endpoints are nested under.</param>
    protected void SetupWebApplication<TEndpointsBuilder>(string endpointsPrefix = "/") where TEndpointsBuilder : EndpointsBuilder, new()
    {
        App = Builder.Build();

        App.UseCors(x => x
            .AllowAnyMethod()
            .AllowAnyHeader()
            .SetIsOriginAllowed(_ => true) // allow any origin
            .AllowCredentials()); // allow credentials
        
        App.MapDefaultEndpoints();
        
        new TEndpointsBuilder().AddEndpoints(App, endpointsPrefix);
        
        App.UseHttpsRedirection();
        
        App.MapOpenApi();
        App.MapScalarApiReference();
    }
    
    /// <summary>Starts the web application and runs until <paramref name="ct"/> is cancelled.</summary>
    /// <param name="ct">Token to stop the application; runs indefinitely if omitted.</param>
    public async Task Run(CancellationToken? ct = null)
    {
        App.Logger.LogInformation("Starting {this}", this);
        await App.RunAsync(ct ?? CancellationToken.None);
    }

    /// <summary>Stops and disposes the underlying web application.</summary>
    public async ValueTask DisposeAsync()
    {
        await App.StopAsync();
        await App.DisposeAsync();
    }
}