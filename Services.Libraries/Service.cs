using Common.Services.Events;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using Services.Libraries.Database;
using Services.Libraries.EventHandlers;
using Services.Libraries.Features;
using Services.Manga.Database;
using Constants = Common.Settings.Constants;

namespace Services.Libraries;

/// <summary>
/// Entry point and bootstrapper for the Libraries service: wires up the <see cref="LibrariesContext"/> and
/// <see cref="MangaContext"/> database contexts, registers the "/libraries" endpoints, subscribes to
/// Tranga events, and runs pending EF Core migrations on startup.
/// </summary>
public sealed class Service : Common.Services.Service
{
    private readonly List<IEventHandler> _eventHandlers = [];

    /// <summary>
    /// Builds and configures the Libraries service web application.
    /// </summary>
    /// <param name="args">Command-line arguments passed through to the ASP.NET Core host builder.</param>
    public Service(string[] args) : base(args)
    {
        Builder.Services.AddDbContext<LibrariesContext>();
        Builder.Services.AddDbContext<MangaContext>();

        Builder.Services.AddScoped<EventPublisher>();

        SetupWebApplication<Endpoints>("/libraries");

        if (!Constants.OpenApiDocumentationRun)
            AddTrangaEventHandlers(App);
        
        if (!Constants.OpenApiDocumentationRun)
        {
            using LibrariesContext context = App.Services.CreateScope().ServiceProvider.GetRequiredService<LibrariesContext>();
            context.Database.MigrateAsync(CancellationToken.None).Wait();
        }
    }

    private void AddTrangaEventHandlers(WebApplication app)
    {
        IChannel channel = app.Services.GetRequiredService<IChannel>();
        _eventHandlers.Add(new ChapterDownloadedHandler(channel, app.Services));
        _eventHandlers.Add(new MangaUpdatedHandler(channel, app.Services));
    }

    /// <summary>
    /// Process entry point: constructs the <see cref="Service"/> and runs it until shutdown.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    public static void Main(string[] args)
    {
        Service service = new (args);
        Task.WaitAll(service.Run());
    }
}