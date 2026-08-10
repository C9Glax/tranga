using Common.Services.Events;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using Services.Notifications.Database;
using Services.Notifications.EventHandlers;
using Services.Notifications.Features;
using Constants = Common.Settings.Constants;

namespace Services.Notifications;

/// <summary>
/// Entry point for the Notifications service, which owns configured notification extensions
/// (Naprise-based channels: Gotify, Telegram, Discord, Ntfy.sh) and dispatches notifications in reaction to Tranga events (e.g. chapter downloaded).
/// </summary>
public sealed class Service : Common.Services.Service
{
    private readonly List<IEventHandler> _eventHandlers = [];

    /// <summary>
    /// Builds the web application, registers the notifications database context and event publisher,
    /// wires up event handlers, and runs pending EF Core migrations (unless running in OpenAPI documentation mode).
    /// </summary>
    /// <param name="args">Command-line arguments passed to the service host.</param>
    public Service(string[] args) : base(args)
    {
        Builder.Services.AddDbContext<NotificationsContext>();

        Builder.Services.AddScoped<EventPublisher>();

        SetupWebApplication<Endpoints>("/notifications");

        if (!Constants.OpenApiDocumentationRun)
            AddTrangaEventHandlers(App);
        
        if (!Constants.OpenApiDocumentationRun)
        {
            using NotificationsContext context = App.Services.CreateScope().ServiceProvider.GetRequiredService<NotificationsContext>();
            context.Database.MigrateAsync(CancellationToken.None).Wait();
        }
    }

    /// <summary>
    /// Registers the RabbitMQ event handlers this service reacts to, e.g. sending notifications when a chapter finishes downloading.
    /// </summary>
    /// <param name="app">The configured web application whose services and RabbitMQ channel are used to construct the handlers.</param>
    private  void AddTrangaEventHandlers(WebApplication app)
    {
        IChannel channel = app.Services.GetRequiredService<IChannel>();
        _eventHandlers.Add(new ChapterDownloadedHandler(channel, app.Services));
    }

    /// <summary>The process entry point; constructs and runs the Notifications service.</summary>
    /// <param name="args">Command-line arguments passed to the service host.</param>
    public static void Main(string[] args)
    {
        Service service = new (args);
        Task.WaitAll(service.Run());
    }
}