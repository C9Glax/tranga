using Common.Services.Events;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using Services.Libraries.Database;
using Services.Libraries.Features;
using Constants = Common.Settings.Constants;

namespace Services.Libraries;

public sealed class Service : Common.Services.Service
{
    private readonly List<IEventHandler> _eventHandlers = [];
    
    public Service(string[] args) : base(args)
    {
        Builder.Services.AddDbContext<LibrariesContext>();

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

    private  void AddTrangaEventHandlers(WebApplication app)
    {
        IChannel channel = app.Services.GetRequiredService<IChannel>();
        
    }

    public static void Main(string[] args)
    {
        Service service = new (args);
        Task.WaitAll(service.Run());
    }
}