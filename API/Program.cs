using System.Reflection;
using API;
using API.Schema.ActionsContext;
using API.Schema.ActionsContext.Actions;
using API.Schema.LibraryContext;
using API.Schema.MangaContext;
using API.Schema.NotificationsContext;
using Asp.Versioning;
using Asp.Versioning.Builder;
using Asp.Versioning.Conventions;
using log4net;
using log4net.Config;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Newtonsoft.Json.Converters;
using Npgsql;

string tranga =
    "\n\n" +
    " _______                                 v2\n" +
    "|_     _|.----..---.-..-----..-----..---.-.\n" +
    "  |   |  |   _||  _  ||     ||  _  ||  _  |\n" +
    "  |___|  |__|  |___._||__|__||___  ||___._|\n" +
    "                             |_____|       \n" +
    $"Built at {BuildInformation.BuildAt} for {BuildInformation.Platform} version {BuildInformation.DotNetSdkVersion}\n" +
    $"branch: {ThisAssembly.Git.Branch} commit: {ThisAssembly.Git.Commit} tag: {ThisAssembly.Git.Tag}\n\n";

XmlConfigurator.ConfigureAndWatch(new FileInfo("Log4Net.config.xml"));
ILog log = LogManager.GetLogger("Startup");
log.Info(tranga);
log.Info("Logger Configured.");

log.Info("Starting up");
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

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

log.Debug("Adding API-Explorer-helpers...");
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
builder.Services.ConfigureOptions<NamedSwaggerGenOptions>();
builder.Services.AddSwaggerGenNewtonsoftSupport().AddSwaggerGen(opt =>
{
    string xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    opt.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

log.Debug("Adding Database-Connection...");
NpgsqlConnectionStringBuilder connectionStringBuilder = new()
{
    Host = Constants.PostgresHost,
    Database = Constants.PostgresDb,
    Username = Constants.PostgresUser,
    Password = Constants.PostgresPassword,
    ConnectionLifetime = 300,
    Timeout = Constants.PostgresConnectionTimeout,
    ReadBufferSize = 65536,
    WriteBufferSize = 65536,
    CommandTimeout = Constants.PostgresCommandTimeout,
    ApplicationName = "Tranga"
};

builder.Services.AddDbContext<MangaContext>(options =>
    options.UseNpgsql(connectionStringBuilder.ConnectionString));
builder.Services.AddDbContext<NotificationsContext>(options =>
    options.UseNpgsql(connectionStringBuilder.ConnectionString));
builder.Services.AddDbContext<LibraryContext>(options =>
    options.UseNpgsql(connectionStringBuilder.ConnectionString));
builder.Services.AddDbContext<ActionsContext>(options =>
    options.UseNpgsql(connectionStringBuilder.ConnectionString));

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});
builder.Services.AddControllers(options =>
{
    options.AllowEmptyInputInBodyModelBinding = true;
}).AddNewtonsoftJson(opts =>
{
    opts.SerializerSettings.Converters.Add(new StringEnumConverter());
});
builder.Services.AddScoped<ILog>(_ => LogManager.GetLogger("API"));

builder.WebHost.UseUrls($"http://*:{TrangaSettings.Port}");

log.Info("Starting app...");
WebApplication app = builder.Build();

app.UseCors("AllowAll");

ApiVersionSet apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(2))
    .ReportApiVersions()
    .Build();

app.UseCors("AllowAll");

log.Debug("Mapping Controllers...");
app.MapControllers()
    .WithApiVersionSet(apiVersionSet)
    .MapToApiVersion(2);

log.Debug("Adding Swagger...");
app.UseSwagger(opts =>
{
    opts.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;
    opts.RouteTemplate = "swagger/{documentName}/swagger.json";
});
app.UseSwaggerUI(opts =>
{
    opts.SwaggerEndpoint("/swagger/v2/swagger.json", "v2");
});

app.UseHttpsRedirection();

try //Connect to DB and apply migrations
{
    log.Debug("Applying Migrations...");
    using (IServiceScope scope = app.Services.CreateScope())
    {
        MangaContext context = scope.ServiceProvider.GetRequiredService<MangaContext>();
        await context.Database.MigrateAsync(CancellationToken.None);

        if (!await context.FileLibraries.AnyAsync())
        {
            await context.FileLibraries.AddAsync(new(TrangaSettings.DefaultDownloadLocation, "Default FileLibrary"),
                CancellationToken.None);
            

            if(await context.Sync(CancellationToken.None, reason: "Add default library") is { success: false } contextException)
                log.ErrorFormat("Failed to save database changes: {0}", contextException.exceptionMessage);
        }
    }

    using (IServiceScope scope = app.Services.CreateScope())
    {
        NotificationsContext context = scope.ServiceProvider.GetRequiredService<NotificationsContext>();
        await context.Database.MigrateAsync(CancellationToken.None);

        int deleted = await context.Notifications.ExecuteDeleteAsync(CancellationToken.None);
        log.DebugFormat("Deleted {0} old notifications.", deleted);
        string[] emojis =
        [
            "(•‿•)", "(づ \u25d5‿\u25d5 )づ", "( \u02d8\u25bd\u02d8)っ\u2668", "=\uff3e\u25cf \u22cf \u25cf\uff3e=",
            "（ΦωΦ）", "(\u272a\u3268\u272a)", "( ﾉ･o･ )ﾉ", "（〜^\u2207^ )〜", "~(\u2267ω\u2266)~", "૮ \u00b4• ﻌ \u00b4• ა",
            "(\u02c3ᆺ\u02c2)", "(=\ud83d\udf66 \u0f1d \ud83d\udf66=)"
        ];
        await context.Notifications.AddAsync(
            new("Tranga Started", emojis[Random.Shared.Next(0, emojis.Length - 1)], NotificationUrgency.High),
            CancellationToken.None);

        if(await context.Sync(CancellationToken.None, reason: "Startup notification") is { success: false } contextException)
            log.ErrorFormat("Failed to save database changes: {0}", contextException.exceptionMessage);
    }

    using (IServiceScope scope = app.Services.CreateScope())
    {
        LibraryContext context = scope.ServiceProvider.GetRequiredService<LibraryContext>();
        await context.Database.MigrateAsync(CancellationToken.None);

        await context.Sync(CancellationToken.None, reason: "Startup library");
    }

    using (IServiceScope scope = app.Services.CreateScope())
    {
        ActionsContext context = scope.ServiceProvider.GetRequiredService<ActionsContext>();
        await context.Database.MigrateAsync(CancellationToken.None);
        context.Actions.Add(new StartupActionRecord());

        if(await context.Sync(CancellationToken.None, reason: "Startup actions") is { success: false } contextException)
            log.ErrorFormat("Failed to save database changes: {0}", contextException.exceptionMessage);
    }
}
catch (Exception e)
{
    log.Fatal("Migrations failed!", e);
    return;
}

log.Info("Starting Tranga.");
Tranga.ServiceProvider = app.Services;
Tranga.StartupTasks();
Tranga.AddDefaultWorkers();

log.Info("Running app.");
await app.RunAsync();