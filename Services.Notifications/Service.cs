using Common.Services.Events;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using Services.Notifications.Database;
using Services.Notifications.EventHandlers;
using Services.Notifications.Features;
using Constants = Common.Settings.Constants;

namespace Services.Notifications;

public sealed class Service : Common.Services.Service
{
    private readonly List<IEventHandler> _eventHandlers = [];
    
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

    private  void AddTrangaEventHandlers(WebApplication app)
    {
        IChannel channel = app.Services.GetRequiredService<IChannel>();
        _eventHandlers.Add(new ChapterDownloadedHandler(channel, app.Services));
    }

    public static void Main(string[] args)
    {
        Service service = new (args);
        Task.WaitAll(service.Run());
    }
}