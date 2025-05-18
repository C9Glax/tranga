using System.Reflection;
using API;
using API.Schema;
using API.Schema.Contexts;
using API.Schema.Jobs;
using API.Schema.MangaConnectors;
using Asp.Versioning;
using Asp.Versioning.Builder;
using Asp.Versioning.Conventions;
using log4net;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

builder.Services.AddApiVersioning(option =>
    {
        option.AssumeDefaultVersionWhenUnspecified = true;
        option.DefaultApiVersion = new ApiVersion(2);
        option.ReportApiVersions = true;
        option.ApiVersionReader = ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader(),
            new QueryStringApiVersionReader("api-version"),
            new HeaderApiVersionReader("X-Version"),
            new MediaTypeApiVersionReader("x-version"));
    })
    .AddMvc(options =>
    {
        options.Conventions.Add(new VersionByNamespaceConvention());
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'V";
        options.SubstituteApiVersionInUrl = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGenNewtonsoftSupport();
builder.Services.AddSwaggerGen(opt =>
{
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    opt.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});
builder.Services.ConfigureOptions<NamedSwaggerGenOptions>();

string ConnectionString = $"Host={Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost:5432"}; " +
                          $"Database={Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "postgres"}; " +
                          $"Username={Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "postgres"}; " +
                          $"Password={Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "postgres"}";

builder.Services.AddDbContext<PgsqlContext>(options =>
    options.UseNpgsql(ConnectionString));
builder.Services.AddDbContext<NotificationsContext>(options =>
    options.UseNpgsql(ConnectionString));
builder.Services.AddDbContext<LibraryContext>(options =>
    options.UseNpgsql(ConnectionString));

builder.Services.AddControllers(options =>
{
    options.AllowEmptyInputInBodyModelBinding = true;
});
builder.Services.AddControllers().AddNewtonsoftJson(opts =>
{
    opts.SerializerSettings.Converters.Add(new StringEnumConverter());
    opts.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
});
builder.Services.AddScoped<ILog>(opts => LogManager.GetLogger("API"));

builder.WebHost.UseUrls("http://*:6531");

var app = builder.Build();

ApiVersionSet apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(2))
    .ReportApiVersions()
    .Build();


app.UseCors("AllowAll");

app.MapControllers()
    .WithApiVersionSet(apiVersionSet)
    .MapToApiVersion(2);

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
            $"/swagger/v2/swagger.json", "v2"); 
});

app.UseHttpsRedirection();

app.UseMiddleware<RequestTimeMiddleware>();

using (IServiceScope scope = app.Services.CreateScope())
{
    PgsqlContext context = scope.ServiceProvider.GetRequiredService<PgsqlContext>();
    context.Database.Migrate();
    
    MangaConnector[] connectors =
    [
        new MangaDex(),
        new ComickIo(),
        new Global(scope.ServiceProvider.GetService<PgsqlContext>()!)
    ];
    MangaConnector[] newConnectors = connectors.Where(c => !context.MangaConnectors.Contains(c)).ToArray();
    context.MangaConnectors.AddRange(newConnectors);
    if (!context.LocalLibraries.Any())
        context.LocalLibraries.Add(new LocalLibrary(TrangaSettings.downloadLocation, "Default Library"));

    context.Jobs.AddRange(context.Jobs.Where(j => j.JobType == JobType.DownloadAvailableChaptersJob)
        .Include(downloadAvailableChaptersJob => ((DownloadAvailableChaptersJob)downloadAvailableChaptersJob).Manga)
        .ToList()
        .Select(dacj => new UpdateChaptersDownloadedJob(((DownloadAvailableChaptersJob)dacj).Manga, 0)));
    context.Jobs.RemoveRange(context.Jobs.Where(j => j.state == JobState.Completed && j.RecurrenceMs < 1));
    foreach (Job job in context.Jobs.Where(j => j.state == JobState.Running))
    {
        job.state = JobState.FirstExecution;
        job.LastExecution = DateTime.UnixEpoch;
    }
    
    context.SaveChanges();
}

using (IServiceScope scope = app.Services.CreateScope())
{
    NotificationsContext context = scope.ServiceProvider.GetRequiredService<NotificationsContext>();
    context.Database.Migrate();
    
    string[] emojis = { "(•‿•)", "(づ \u25d5‿\u25d5 )づ", "( \u02d8\u25bd\u02d8)っ\u2668", "=\uff3e\u25cf \u22cf \u25cf\uff3e=", "（ΦωΦ）", "(\u272a\u3268\u272a)", "( ﾉ･o･ )ﾉ", "（〜^\u2207^ )〜", "~(\u2267ω\u2266)~","૮ \u00b4• ﻌ \u00b4• ა", "(\u02c3ᆺ\u02c2)", "(=\ud83d\udf66 \u0f1d \ud83d\udf66=)"};
    context.Notifications.Add(new Notification("Tranga Started", emojis[Random.Shared.Next(0, emojis.Length - 1)], NotificationUrgency.High));
    
    context.SaveChanges();
}


TrangaSettings.Load();
Tranga.StartLogger();
Tranga.RemoveStaleFiles(app.Services);
Tranga.JobStarterThread.Start(app.Services);
//Tranga.NotificationSenderThread.Start(app.Services); //TODO RE-ENABLE

app.UseCors("AllowAll");

app.Run();