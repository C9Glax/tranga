using Scalar.AspNetCore;

namespace Common.Services;

public abstract class Service(string[] args) : IAsyncDisposable
{
    protected WebApplicationBuilder Builder = WebApplication.CreateBuilder(args).SetupWebApplicationBuilder();
    protected WebApplication App { get; set; }

    protected void SetupWebApplication<TEndpointsBuilder>() where TEndpointsBuilder : IEndpointsBuilder, new()
    {
        App = Builder.Build();

        App.UseCors(x => x
            .AllowAnyMethod()
            .AllowAnyHeader()
            .SetIsOriginAllowed(_ => true) // allow any origin
            .AllowCredentials()); // allow credentials

        new TEndpointsBuilder().AddEndpoints(App);
        
        App.UseHttpsRedirection();
        
        App.MapOpenApi();
        App.MapScalarApiReference();
    }
    
    public async Task Run(CancellationToken? ct = null)
    {
        await App.RunAsync(ct ?? CancellationToken.None);
    }

    public async ValueTask DisposeAsync()
    {
        await App.StopAsync();
        await App.DisposeAsync();
    }
}