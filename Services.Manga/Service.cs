using Common.Database;
using NSwagClients.GeneratedClients.TasksService;
using Services.Manga.Database;
using Services.Manga.Features;
using Task = System.Threading.Tasks.Task;

namespace Services.Manga;

public sealed class Service : Common.Services.Service
{
    public Service(string[] args) : base(args)
    {
        Builder.Services.AddDbContext<MangaContext>(opts =>
            opts.Configure(DatabaseContextOptionsBuilder.DbType.Postgresql));

        Builder.Configuration.GetSection("Services.Tasks").GetValue<string>("baseUrl");
        Builder.Services.AddSingleton<MyTasksServiceApiClient>();

        SetupWebApplication<Endpoints>("/mangas");

        using MangaContext context = App.Services.CreateScope().ServiceProvider.GetRequiredService<MangaContext>();
        Task.WaitAll(context.ApplyMigrations());
    }

    public static void Main(string[] args)
    {
        Service service = new (args);
        Task.WaitAll(service.Run());
    }
    
    internal class MyTasksServiceApiClient(IConfiguration configuration) : TasksServiceApiClient(GetBaseUrl(configuration), new HttpClient())
    {
        private static string GetBaseUrl(IConfiguration configuration) =>
            configuration.GetSection("Services.Tasks").GetValue<string>("baseUrl") ??
            Environment.GetEnvironmentVariable("SERVICES_TASKS_BASE_URL") ?? 
            throw new Exception("Missing config Section and Key 'Services.Tasks': { 'baseUrl': string }");
    }
}