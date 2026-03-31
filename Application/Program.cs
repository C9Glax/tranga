using Database;
using Database.MangaContext;
using Application.Services;
using Application.Workers;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<MangaContext>(opts => opts.Configure(null), ServiceLifetime.Scoped, ServiceLifetime.Singleton);

builder.Services.AddHostedService<ApiService>();

builder.Services.AddScoped<ApplyMigrationsWorker>();

IHost app = builder.Build();

await app.Services.CreateScope().ServiceProvider.GetRequiredService<ApplyMigrationsWorker>().StartAsync(CancellationToken.None);

app.Run();