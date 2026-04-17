using Scalar.AspNetCore;

namespace Common.Services;

public abstract class Service : IAsyncDisposable
{
    protected WebApplicationBuilder Builder { get; init; }
    protected WebApplication App { get; set; }

    public Service(string[] args)
    {
        Builder = WebApplication.CreateBuilder(args).SetupWebApplicationBuilder();
        
        Builder.Logging.ClearProviders();
        Builder.Logging.AddConsole();
    }

    protected void SetupWebApplication<TEndpointsBuilder>(string endpointsPrefix = "/") where TEndpointsBuilder : EndpointsBuilder, new()
    {
        App = Builder.Build();

        App.UseCors(x => x
            .AllowAnyMethod()
            .AllowAnyHeader()
            .SetIsOriginAllowed(_ => true) // allow any origin
            .AllowCredentials()); // allow credentials

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