using Common.Services.Events;
using Common.Settings;
using Scalar.AspNetCore;

namespace Common.Services;

public abstract class Service : IAsyncDisposable
{
    protected WebApplicationBuilder Builder { get; init; }
    protected WebApplication App { get; set; }

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
    
    public async Task Run(CancellationToken? ct = null)
    {
        App.Logger.LogInformation("Starting {this}", this);
        await App.RunAsync(ct ?? CancellationToken.None);
    }

    public async ValueTask DisposeAsync()
    {
        await App.StopAsync();
        await App.DisposeAsync();
    }
}